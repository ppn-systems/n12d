import argparse
import os
import datetime
import pandas as pd
import numpy as np
import plotly.graph_objects as plotly_go
from plotly.subplots import make_subplots
from jinja2 import Environment, FileSystemLoader

from src.parser import TelemetryParser, SchemaError
from src.analyzers import detect_spikes, detect_leak, detect_attack_window, compute_endpoints, detect_worker_imbalance, compute_task_details
import urllib.request

def generate_charts(df, attack_window, parser=None):
    charts_html = []
    
    # Helper to add attack window rect
    def add_attack_rect(fig):
        if attack_window:
            fig.add_vrect(
                x0=attack_window.start_time, x1=attack_window.end_time,
                fillcolor="red", opacity=0.15, layer="below", line_width=0,
                annotation_text="DDoS Attack Window", annotation_position="top left",
                annotation_font_color="red"
            )
            
    # Chart 1: Network & Connections
    if 'TotalConnections' in df.columns:
        fig1 = make_subplots(specs=[[{"secondary_y": True}]])
        fig1.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TotalConnections'], name="Connections", line=dict(color="#3b82f6")), secondary_y=False)
        if 'IngressBytesPerSecond' in df.columns:
            fig1.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['IngressBytesPerSecond']/(1024*1024), name="Ingress (MB/s)", line=dict(color="#10b981", dash="dot")), secondary_y=True)
            fig1.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['EgressBytesPerSecond']/(1024*1024), name="Egress (MB/s)", line=dict(color="#8b5cf6", dash="dot")), secondary_y=True)
        fig1.update_layout(title="Network Overview", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig1)
        charts_html.append(fig1.to_html(full_html=False, include_plotlyjs=False))
        
    # Chart 2: Memory & GC Pressure
    if 'WorkingSetMB' in df.columns and 'ManagedHeapMB' in df.columns:
        fig2 = make_subplots(specs=[[{"secondary_y": True}]])
        fig2.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['WorkingSetMB'], name="Working Set (MB)", fill='tozeroy', line=dict(color="#f59e0b")), secondary_y=False)
        fig2.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['ManagedHeapMB'], name="Managed Heap (MB)", line=dict(color="#ef4444")), secondary_y=False)
        if 'Process.GCGen2' in df.columns:
            fig2.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['Process.GCGen2'], name="GC Gen 2", mode='markers', marker=dict(size=4, color="#10b981")), secondary_y=True)
        fig2.update_layout(title="Memory & GC Pressure", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig2)
        charts_html.append(fig2.to_html(full_html=False, include_plotlyjs=False))

    # Chart 3: Threads & Workers
    if 'Threads' in df.columns:
        fig3 = make_subplots(specs=[[{"secondary_y": True}]])
        fig3.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['Threads'], name="Total Threads", line=dict(color="#6366f1")), secondary_y=False)
        if 'ThreadsRunning' in df.columns:
             fig3.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['ThreadsRunning'], name="Running Threads", line=dict(color="#ec4899")), secondary_y=False)
        if 'WorkersRunning' in df.columns:
             fig3.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['WorkersRunning'], name="Running Workers", line=dict(color="#14b8a6")), secondary_y=False)
        fig3.update_layout(title="Threads & Workers Utilization", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig3)
        charts_html.append(fig3.to_html(full_html=False, include_plotlyjs=False))

    # Chart 4: Dispatch Pipeline
    if 'PendingPerPriority.HIGH' in df.columns:
        fig4 = plotly_go.Figure()
        fig4.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['PendingPerPriority.URGENT'], name="Pending URGENT", stackgroup='one', line=dict(color="#ef4444")))
        fig4.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['PendingPerPriority.HIGH'], name="Pending HIGH", stackgroup='one', line=dict(color="#f59e0b")))
        if 'PipelineMetrics.ActiveExecutions' in df.columns:
            fig4.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['PipelineMetrics.ActiveExecutions'], name="Active Executions", line=dict(color="#3b82f6", width=2)))
        fig4.update_layout(title="Dispatch Pipeline Queues", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig4)
        charts_html.append(fig4.to_html(full_html=False, include_plotlyjs=False))

    # Chart 5: Connection Guard
    if 'TotalConcurrent' in df.columns and 'TotalRejections' in df.columns:
        fig5 = make_subplots(specs=[[{"secondary_y": True}]])
        fig5.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TotalConcurrent'], name="Guard Concurrent", line=dict(color="#10b981")), secondary_y=False)
        fig5.add_trace(plotly_go.Bar(x=df['Timestamp'], y=df['TotalRejections'], name="Rejections", marker_color="#ef4444"), secondary_y=True)
        fig5.update_layout(title="Connection Guard Health", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig5)
        charts_html.append(fig5.to_html(full_html=False, include_plotlyjs=False))
        
    # Chart 6: Buffer Performance (Enhanced Stacked Area)
    buffer_breakdown_df = parser.get_buffer_breakdown() if parser is not None else pd.DataFrame()
    if not buffer_breakdown_df.empty:
        fig6 = make_subplots(specs=[[{"secondary_y": True}]])
        sizes_sorted = buffer_breakdown_df.sort_values('BufferSize')['SizeLabel'].unique()
        colors = ["#3b82f6", "#10b981", "#8b5cf6", "#f59e0b", "#ec4899", "#14b8a6", "#ef4444", "#a855f7", "#06b6d4"]
        
        for i, sl in enumerate(sizes_sorted):
            sub = buffer_breakdown_df[buffer_breakdown_df['SizeLabel'] == sl]
            mem_mb = (sub['InUse'] * sub['BufferSize']) / (1024 * 1024)
            color = colors[i % len(colors)]
            fig6.add_trace(plotly_go.Scatter(
                x=sub['Timestamp'],
                y=mem_mb,
                name=f"In-Use {sl} (MB)",
                stackgroup='one',
                line=dict(color=color, width=0.5),
                fill='tonexty',
                mode='lines'
            ), secondary_y=False)
            
        if 'HitRate' in df.columns:
            fig6.add_trace(plotly_go.Scatter(
                x=df['Timestamp'],
                y=df['HitRate'],
                name="Buffer Hit Rate (%)",
                line=dict(color="#22c55e", width=2.5),
                mode='lines'
            ), secondary_y=True)
            
        if 'TotalExpands' in df.columns:
            exp_diff = df['TotalExpands'].diff().fillna(0)
            if exp_diff.sum() > 0:
                fig6.add_trace(plotly_go.Bar(
                    x=df['Timestamp'],
                    y=exp_diff,
                    name="Bucket Expands",
                    marker_color="#ef4444",
                    opacity=0.6
                ), secondary_y=True)
                
        fig6.update_layout(title="Buffer Memory Usage per Bucket Size (MB)", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20), hovermode="x unified")
        fig6.update_yaxes(title_text="Memory (MB)", secondary_y=False)
        fig6.update_yaxes(title_text="Hit Rate / Expands", secondary_y=True, range=[0, 105])
        charts_html.append(fig6.to_html(full_html=False, include_plotlyjs=False))
    elif 'PeakMemoryUsageBytes' in df.columns:
        fig6 = make_subplots(specs=[[{"secondary_y": True}]])
        fig6.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['PeakMemoryUsageBytes']/(1024*1024), name="Buffer Peak Memory (MB)", fill='tozeroy', line=dict(color="#8b5cf6")), secondary_y=False)
        if 'HitRate' in df.columns:
            fig6.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['HitRate'], name="Buffer Hit Rate", line=dict(color="#22c55e")), secondary_y=True)
        fig6.update_layout(title="Buffer Pools Performance", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        charts_html.append(fig6.to_html(full_html=False, include_plotlyjs=False))
    else:
        charts_html.append("<div>No Buffer Pool data available</div>")
        
    # Chart 7: Protocol Messages
    if 'TCP.TotalMessages' in df.columns:
        fig7 = plotly_go.Figure()
        fig7.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TCP.TotalMessages'], name="TCP Msgs", line=dict(color="#3b82f6")))
        fig7.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['WEBSOCKET.TotalMessages'], name="WS Msgs", line=dict(color="#ec4899")))
        if 'TCP.TotalErrors' in df.columns:
            fig7.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TCP.TotalErrors'], name="TCP Errors", line=dict(color="#ef4444", dash="dot")))
        fig7.update_layout(title="Protocol Messages & Errors", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig7)
        charts_html.append(fig7.to_html(full_html=False, include_plotlyjs=False))
        
    # Chart 8: Execution Percentiles
    if 'P95WorkerExecutionTimeMs' in df.columns:
        fig8 = plotly_go.Figure()
        fig8.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['AverageWorkerExecutionTimeMs'], name="Avg Latency", line=dict(color="#22c55e")))
        fig8.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['P95WorkerExecutionTimeMs'], name="P95 Latency", line=dict(color="#f59e0b")))
        fig8.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['P99WorkerExecutionTimeMs'], name="P99 Latency", line=dict(color="#ef4444")))
        fig8.update_layout(title="Task Latency Percentiles (ms)", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        add_attack_rect(fig8)
        charts_html.append(fig8.to_html(full_html=False, include_plotlyjs=False))

    # Chart 9: Object Pool Health Over Time
    fig9 = make_subplots(specs=[[{"secondary_y": True}]])
    if 'TotalGetOperations' in df.columns:
        gets_delta = df['TotalGetOperations'].diff().fillna(0)
        fig9.add_trace(plotly_go.Bar(x=df['Timestamp'], y=gets_delta, name="Get Operations (Delta)", marker_color="#3b82f6", opacity=0.7), secondary_y=False)
        
    if 'TotalLeaked' in df.columns:
        fig9.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TotalLeaked'], name="Total Leaked Objects", line=dict(color="#ef4444", width=2.5, dash="dot"), mode='lines'), secondary_y=False)
        
    if 'CacheHitRate' in df.columns:
        fig9.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['CacheHitRate'], name="Pool Cache Hit Rate (%)", line=dict(color="#10b981", width=2.5), mode='lines'), secondary_y=True)
        
    if 'UnhealthyPoolCount' in df.columns:
        fig9.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['UnhealthyPoolCount'], name="Unhealthy Pool Count", line=dict(color="#f59e0b", width=1.5), mode='lines'), secondary_y=False)
        
    fig9.update_layout(title="Object Pool Diagnostics & Hit Rate", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20), hovermode="x unified")
    fig9.update_yaxes(title_text="Operations / Leaks", secondary_y=False)
    fig9.update_yaxes(title_text="Hit Rate (%)", secondary_y=True, range=[0, 105])
    charts_html.append(fig9.to_html(full_html=False, include_plotlyjs=False))

    # Chart 10: Object Pool Throughput (Lifecycle Flow)
    fig10 = plotly_go.Figure()
    if 'TotalCreated' in df.columns:
        fig10.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TotalCreated'].diff().fillna(0), name="Created Delta", line=dict(color="#f59e0b", width=2), mode='lines'))
    if 'TotalDisposed' in df.columns:
        fig10.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TotalDisposed'].diff().fillna(0), name="Disposed Delta", line=dict(color="#ef4444", width=2), mode='lines'))
    if 'TotalReturnOperations' in df.columns:
        fig10.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['TotalReturnOperations'].diff().fillna(0), name="Returns Delta", line=dict(color="#10b981", width=2), mode='lines'))
        
    fig10.update_layout(title="Object Pool Lifecycle Throughput (Deltas)", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20), hovermode="x unified")
    charts_html.append(fig10.to_html(full_html=False, include_plotlyjs=False))

    # Chart 11: Instance Cache Performance
    instance_timeline_df = parser.get_instance_timeline() if parser is not None else pd.DataFrame()
    if not instance_timeline_df.empty:
        fig11 = make_subplots(specs=[[{"secondary_y": True}]])
        fig11.add_trace(plotly_go.Scatter(x=instance_timeline_df['Timestamp'], y=instance_timeline_df['CachedInstanceCount'], name="Cached Instance Count", fill='tozeroy', line=dict(color="#8b5cf6", width=2), mode='lines'), secondary_y=False)
        creations_delta = instance_timeline_df['InstanceCreationCount'].diff().fillna(0)
        fig11.add_trace(plotly_go.Bar(x=instance_timeline_df['Timestamp'], y=creations_delta, name="New Activations (Delta)", marker_color="#ef4444", opacity=0.8), secondary_y=False)
        fig11.add_trace(plotly_go.Scatter(x=instance_timeline_df['Timestamp'], y=instance_timeline_df['CacheEfficiency'], name="Instance Cache Efficiency (%)", line=dict(color="#10b981", width=2.5), mode='lines'), secondary_y=True)
        
        fig11.update_layout(title="Instance Cache Diagnostics & Activation Activity", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20), hovermode="x unified")
        fig11.update_yaxes(title_text="Cached Instances / Creations", secondary_y=False)
        fig11.update_yaxes(title_text="Cache Efficiency (%)", secondary_y=True, range=[0, 105])
        charts_html.append(fig11.to_html(full_html=False, include_plotlyjs=False))
    else:
        charts_html.append("<div>No Instance Cache data available</div>")

    return charts_html

def format_top_endpoints_html(top_endpoints):
    if top_endpoints.empty:
        return '<tr><td colspan="4" class="p-3 text-center">No endpoints found</td></tr>'
    
    html = ""
    for idx, row in top_endpoints.iterrows():
        html += f"""
        <tr class="hover:bg-gray-800 transition-colors">
            <td class="p-3 border-t border-gray-700 font-mono text-gray-300">{row['endpoint']}</td>
            <td class="p-3 border-t border-gray-700 text-right text-amber-400 font-medium">{row['cumulative_pending']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right text-red-400 font-medium">{row['peak_pending']:,}</td>
            <td class="p-3 border-t border-gray-700 text-gray-400">{row['peak_at']}</td>
        </tr>
        """
    return html

def format_top_pools_html(top_pools):
    if top_pools.empty:
        return '<tr><td colspan="7" class="p-3 text-center">No active pools found</td></tr>'
    
    html = ""
    for idx, row in top_pools.iterrows():
        status_color = "text-green-400" if row['status'] == "OK" else "text-red-400 font-bold"
        html += f"""
        <tr class="hover:bg-gray-800 transition-colors">
            <td class="p-3 border-t border-gray-700 font-mono text-gray-300">{row['pool_type']}</td>
            <td class="p-3 border-t border-gray-700 text-right text-gray-400">{row['available']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right text-gray-400">{row['max_capacity']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right text-amber-400 font-medium">{row['outstanding']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right text-blue-400 font-medium">{row['gets']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right text-purple-400">{row['hit_rate']:.1f}%</td>
            <td class="p-3 border-t border-gray-700 {status_color}">{row['status']}</td>
        </tr>
        """
    return html

def export_html(parser, output_path):
    df = parser.get_merged_data()
    top_endpoints = parser.get_top_endpoints()
    top_pools = parser.get_top_pools()
    
    # Run Advanced Analyzers
    attack_window = detect_attack_window(df.set_index('Timestamp')['TotalConnections']) if 'TotalConnections' in df.columns else None
    
    endpoints_df = compute_endpoints(parser.get_json_series('connection_guard_metrics.csv_TopEndpoints'), attack_window)
    rec_df, work_df = compute_task_details(parser.get_json_series('tasks_metrics.csv_Recurring'), parser.get_json_series('tasks_metrics.csv_TopRunningWorkers'))
    worker_anomalies = detect_worker_imbalance(parser.get_json_series('tasks_metrics.csv_TopRunningWorkers'))
    
    # Anomalies collection
    anomalies = []
    if 'TotalConnections' in df.columns:
        spikes = detect_spikes(df.set_index('Timestamp')['TotalConnections'])
        anomalies.extend([s.to_dict() for s in spikes])
    
    anomalies.extend([w.to_dict() for w in worker_anomalies])
        
    leak_result = None
    if 'ManagedHeapMB' in df.columns:
        leak_res = detect_leak(df.set_index('Timestamp')['ManagedHeapMB'])
        leak_result = {
            'is_leak': leak_res.is_leak,
            'message': leak_res.message
        }
        
    # Check for Object Pool Leaks & Starvation
    if 'TotalLeaked' in df.columns:
        max_leaked = df['TotalLeaked'].max()
        if max_leaked > 0:
            anomalies.append({
                'timestamp': str(df.loc[df['TotalLeaked'] > 0, 'Timestamp'].iloc[0]) if not df[df['TotalLeaked'] > 0].empty else "Unknown",
                'severity': 'CRITICAL',
                'message': f"Object Pool Leak detected! Max leaked objects count: {max_leaked:.0f} objects. Verify that all pool leases are properly returned.",
                'value': max_leaked,
                'expected_value': 0,
                'algorithm': 'Threshold Check'
            })
            
    if 'CacheHitRate' in df.columns:
        min_hit_rate = df['CacheHitRate'].min()
        if min_hit_rate < 90.0:
            anomalies.append({
                'timestamp': str(df.loc[df['CacheHitRate'] < 90.0, 'Timestamp'].iloc[0]) if not df[df['CacheHitRate'] < 90.0].empty else "Unknown",
                'severity': 'CRITICAL',
                'message': f"Object Pool Starvation Critical! Cache hit rate dropped to {min_hit_rate:.1f}%. Consider increasing DefaultMaxPoolSize.",
                'value': min_hit_rate,
                'expected_value': 98.0,
                'algorithm': 'Threshold Check'
            })
        elif min_hit_rate < 98.0:
            anomalies.append({
                'timestamp': str(df.loc[df['CacheHitRate'] < 98.0, 'Timestamp'].iloc[0]) if not df[df['CacheHitRate'] < 98.0].empty else "Unknown",
                'severity': 'WARNING',
                'message': f"Object Pool Starvation Warning! Cache hit rate dropped to {min_hit_rate:.1f}%. Consider increasing DefaultMaxPoolSize.",
                'value': min_hit_rate,
                'expected_value': 98.0,
                'algorithm': 'Threshold Check'
            })
            
    # Calculate Instance Cache KPIs and warnings
    instance_cache_eff_val = 100.0
    if 'InstanceCacheHitCount' in df.columns and 'InstanceCreationCount' in df.columns:
        last_hit = df['InstanceCacheHitCount'].iloc[-1]
        last_creation = df['InstanceCreationCount'].iloc[-1]
        total_calls = last_hit + last_creation
        instance_cache_eff_val = (last_hit / total_calls * 100) if total_calls > 0 else 100.0
        
        # Instance cache anomalies
        if 'InstanceCreationCount' in df.columns:
            first_creation = df['InstanceCreationCount'].iloc[0]
            creation_during_run = last_creation - first_creation
            if creation_during_run > 0:
                anomalies.append({
                    'timestamp': str(df.loc[df['InstanceCreationCount'].diff() > 0, 'Timestamp'].iloc[0]) if not df[df['InstanceCreationCount'].diff() > 0].empty else "Unknown",
                    'severity': 'WARNING',
                    'message': f"High instance creation rate detected! {creation_during_run} new instances created during the run. Cache misses are causing activator allocations.",
                    'value': creation_during_run,
                    'expected_value': 0,
                    'algorithm': 'Delta Check'
                })
                
        if instance_cache_eff_val < 90.0:
            anomalies.append({
                'timestamp': str(df['Timestamp'].iloc[-1]),
                'severity': 'CRITICAL',
                'message': f"Cache effectiveness appears low! Estimated cache efficiency is {instance_cache_eff_val:.1f}%. High instance allocations are bypassing the cache.",
                'value': instance_cache_eff_val,
                'expected_value': 98.0,
                'algorithm': 'Threshold Check'
            })
        elif instance_cache_eff_val < 98.0:
            anomalies.append({
                'timestamp': str(df['Timestamp'].iloc[-1]),
                'severity': 'WARNING',
                'message': f"Cache effectiveness appears low! Estimated cache efficiency is {instance_cache_eff_val:.1f}%. Watch for instance lifecycle pressure.",
                'value': instance_cache_eff_val,
                'expected_value': 98.0,
                'algorithm': 'Threshold Check'
            })

    # Calculate summary KPIs
    kpis = {
        'duration': str(df['Timestamp'].max() - df['Timestamp'].min()).split('.')[0],
        'peak_connections': f"{df['TotalConnections'].max():,}" if 'TotalConnections' in df.columns else "N/A",
        'peak_memory': f"{df['WorkingSetMB'].max():.0f} MB" if 'WorkingSetMB' in df.columns else "N/A",
        'peak_threads': f"{df['Threads'].max():.0f}" if 'Threads' in df.columns else "N/A",
        'avg_cache_hit': f"{df['CacheHitRate'].mean():.1f}%" if 'CacheHitRate' in df.columns else "N/A",
        'instance_cache_eff': f"{instance_cache_eff_val:.1f}%" if 'InstanceCacheHitCount' in df.columns else "N/A",
        'total_rejections': f"{df['TotalRejections'].max():,}" if 'TotalRejections' in df.columns else "0",
        'total_messages': f"{df['TCP.TotalMessages'].max() + df['WEBSOCKET.TotalMessages'].max():,}" if 'TCP.TotalMessages' in df.columns else "N/A",
        'p99_latency': f"{df['P99WorkerExecutionTimeMs'].max():.1f} ms" if 'P99WorkerExecutionTimeMs' in df.columns else "N/A"
    }
        
    # Generate charts
    charts = generate_charts(df, attack_window, parser)
    
    top_endpoints_html = format_top_endpoints_html(top_endpoints)
    top_pools_html = format_top_pools_html(top_pools)
    
    # Raw lists for Jinja looping
    top_pools_list = []
    if not top_pools.empty:
        top_pools_list = top_pools.to_dict(orient='records')
        
    instances_list = []
    has_instances = False
    instances_file = next((f for f in parser.raw_dfs.keys() if 'instances' in f), None)
    if instances_file:
        from src.analyzers import compute_cached_instances
        cached_inst_df = compute_cached_instances(parser.raw_dfs[instances_file])
        if not cached_inst_df.empty:
            instances_list = cached_inst_df.to_dict(orient='records')
            has_instances = True
            
    # Build Endpoints Table (from Connection Guard)
    endpoints_guard_html = ""
    for _, row in endpoints_df.iterrows():
        risk_color = "text-red-400 font-bold" if row['Risk'] == "High" or row['Risk'] == "Suspected Attacker" else "text-gray-400"
        endpoints_guard_html += f"""
        <tr class="hover:bg-gray-800 transition-colors">
            <td class="p-3 border-t border-gray-700 font-mono text-gray-300">{row['Address']}</td>
            <td class="p-3 border-t border-gray-700 text-right text-blue-400 font-medium">{row['CurrentConnections']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right text-amber-400 font-medium">{row['TotalConnectionsToday']:,}</td>
            <td class="p-3 border-t border-gray-700 text-right {risk_color}">{row['Risk']}</td>
        </tr>
        """
        
    # Build Workers Table
    workers_html = ""
    for _, row in work_df.iterrows():
        status_color = "text-green-400" if row['IsRunning'] == "Running" else "text-gray-500"
        workers_html += f"""
        <tr class="hover:bg-gray-800 transition-colors">
            <td class="p-3 border-t border-gray-700 font-mono text-gray-300">{row['Worker']}</td>
            <td class="p-3 border-t border-gray-700 text-right text-blue-400 font-medium">{row['Progress']:,}</td>
            <td class="p-3 border-t border-gray-700 {status_color}">{row['IsRunning']}</td>
            <td class="p-3 border-t border-gray-700 text-gray-400">{row['LastRun']}</td>
        </tr>
        """
        
    # Build Recurring Table
    recurring_html = ""
    for _, row in rec_df.iterrows():
        status_color = "text-green-400" if row['Status'] == "Running" else "text-gray-500"
        recurring_html += f"""
        <tr class="hover:bg-gray-800 transition-colors">
            <td class="p-3 border-t border-gray-700 font-mono text-gray-300">{row['Task Name']}</td>
            <td class="p-3 border-t border-gray-700 text-right text-blue-400 font-medium">{row['Executions']:,}</td>
            <td class="p-3 border-t border-gray-700 {status_color}">{row['Status']}</td>
        </tr>
        """
    
    plotly_js_url = "https://cdn.plot.ly/plotly-3.5.0.min.js"
    try:
        req = urllib.request.Request(plotly_js_url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req) as response:
            plotly_js_content = response.read().decode('utf-8')
    except Exception as e:
        print(f"Warning: Could not download plotly js for offline embedding. Using CDN fallback. Error: {e}")
        plotly_js_content = f"// Failed to load plotly offline. Using CDN.\n// Error: {e}"
    
    # 3. Render Jinja
    env = Environment(loader=FileSystemLoader(os.path.join(os.path.dirname(__file__), 'templates')))
    template = env.get_template('report_template.html')
    
    html_out = template.render(
        timestamp=datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        session_path=parser.metrics_dir,
        kpis=kpis,
        attack_window=attack_window,
        anomalies=anomalies,
        leak_result=leak_result,
        top_endpoints=not top_endpoints.empty,
        top_endpoints_html=top_endpoints_html,
        has_guard_endpoints=not endpoints_df.empty,
        endpoints_guard_html=endpoints_guard_html,
        has_workers=not work_df.empty,
        workers_html=workers_html,
        has_recurring=not rec_df.empty,
        recurring_html=recurring_html,
        top_pools=not top_pools.empty,
        top_pools_html=top_pools_html,
        top_pools_list=top_pools_list,
        has_instances=has_instances,
        instances_list=instances_list,
        charts=charts,
        plotly_js_content=plotly_js_content
    )
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(html_out)
    print(f"HTML Report generated at: {output_path}")

def main():
    parser_args = argparse.ArgumentParser(description="Nalix Telemetry Analyzer CLI")
    parser_args.add_argument("--dir", required=True, help="Directory containing CSV metrics")
    parser_args.add_argument("--output", default="report.html", help="Output file path (default: report.html)")
    parser_args.add_argument("--top-n", type=int, default=10, help="Top N endpoints to display")
    
    args = parser_args.parse_args()
    
    print(f"[NalixAnalyzer] Loading telemetry from {args.dir}...")
    telemetry = TelemetryParser(args.dir)
    try:
        telemetry.validate_and_load()
    except SchemaError as e:
        print(f"[Error] {e}")
        return
        
    print("[NalixAnalyzer] Processing and aligning data...")
    telemetry.process(top_n=args.top_n)
    
    out_html = args.output if args.output.endswith('.html') else 'report.html'
    export_html(telemetry, out_html)

if __name__ == "__main__":
    main()
