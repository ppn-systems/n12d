import argparse
import os
import datetime
import plotly.graph_objects as plotly_go
from plotly.subplots import make_subplots
from jinja2 import Environment, FileSystemLoader

from src.parser import TelemetryParser, SchemaError
from src.analyzers import detect_spikes, detect_leak
# note: compute_correlation_matrix was imported but not used, so we can omit or keep it

def generate_charts(df):
    charts_html = []
    
    # Chart 1: Connections & Throughput
    if 'TotalConnections' in df.columns:
        fig1 = make_subplots(specs=[[{"secondary_y": True}]])
        fig1.add_trace(
            plotly_go.Scatter(x=df['Timestamp'], y=df['TotalConnections'], name="Connections", line=dict(color="#3b82f6")),
            secondary_y=False,
        )
        if 'IngressBytesPerSecond' in df.columns and 'EgressBytesPerSecond' in df.columns:
            fig1.add_trace(
                plotly_go.Scatter(x=df['Timestamp'], y=df['IngressBytesPerSecond']/(1024*1024), name="Ingress (MB/s)", line=dict(color="#10b981", dash="dot")),
                secondary_y=True,
            )
            fig1.add_trace(
                plotly_go.Scatter(x=df['Timestamp'], y=df['EgressBytesPerSecond']/(1024*1024), name="Egress (MB/s)", line=dict(color="#8b5cf6", dash="dot")),
                secondary_y=True,
            )
        fig1.update_layout(title="Network Overview", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        charts_html.append(fig1.to_html(full_html=False, include_plotlyjs='cdn'))
        
    # Chart 2: Memory & GC
    if 'WorkingSetMB' in df.columns and 'ManagedHeapMB' in df.columns:
        fig2 = make_subplots(specs=[[{"secondary_y": True}]])
        fig2.add_trace(
            plotly_go.Scatter(x=df['Timestamp'], y=df['WorkingSetMB'], name="Working Set (MB)", fill='tozeroy', line=dict(color="#f59e0b")),
            secondary_y=False,
        )
        fig2.add_trace(
            plotly_go.Scatter(x=df['Timestamp'], y=df['ManagedHeapMB'], name="Managed Heap (MB)", line=dict(color="#ef4444")),
            secondary_y=False,
        )
        if 'Process.GCGen0' in df.columns:
            fig2.add_trace(
                plotly_go.Scatter(x=df['Timestamp'], y=df['Process.GCGen0'], name="GC Gen 0", mode='markers', marker=dict(size=5, color="#10b981")),
                secondary_y=True,
            )
        fig2.update_layout(title="Resource Health (Memory & GC)", template="plotly_dark", height=400, margin=dict(l=20, r=20, t=40, b=20))
        charts_html.append(fig2.to_html(full_html=False, include_plotlyjs='cdn'))
        
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
    
    # 1. Math Analysis & Anomalies
    anomalies = []
    if 'TotalConnections' in df.columns:
        spikes = detect_spikes(df.set_index('Timestamp')['TotalConnections'])
        anomalies.extend([s.to_dict() for s in spikes])
        
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
                'severity': 'WARNING',
                'message': f"Object Pool Starvation Warning! Cache hit rate dropped to {min_hit_rate:.1f}%. Consider increasing DefaultMaxPoolSize.",
                'value': min_hit_rate,
                'expected_value': 100.0,
                'algorithm': 'Threshold Check'
            })
        
    # 2. Generate charts
    charts = generate_charts(df)
    top_endpoints_html = format_top_endpoints_html(top_endpoints)
    top_pools_html = format_top_pools_html(top_pools)
    
    # 3. Render Jinja
    env = Environment(loader=FileSystemLoader(os.path.join(os.path.dirname(__file__), 'templates')))
    template = env.get_template('report_template.html')
    
    html_out = template.render(
        timestamp=datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        session_path=parser.metrics_dir,
        anomalies=anomalies,
        leak_result=leak_result,
        top_endpoints=not top_endpoints.empty,
        top_endpoints_html=top_endpoints_html,
        top_pools=not top_pools.empty,
        top_pools_html=top_pools_html,
        charts=charts
    )
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(html_out)
    print(f"HTML Report generated at: {output_path}")

def export_md(parser, output_path):
    df = parser.get_merged_data()
    top_endpoints = parser.get_top_endpoints()
    top_pools = parser.get_top_pools()
    
    md_content = f"# Nalix Performance Diagnostics Report\n> Generated at: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n\n"
    
    # Top Endpoints
    md_content += "## Top-10 Congested Endpoints (Pending Queues)\n"
    if top_endpoints.empty:
        md_content += "No data available.\n\n"
    else:
        md_content += "| Rank | Endpoint | Total Cumulative Pending | Peak Pending | Peak Time |\n"
        md_content += "| :---: | :--- | :---: | :---: | :---: |\n"
        for i, row in top_endpoints.iterrows():
            md_content += f"| {i+1} | `{row['endpoint']}` | {row['cumulative_pending']:,} | {row['peak_pending']:,} | {row['peak_at']} |\n"
        md_content += "\n"
        
    # Top Object Pools
    md_content += "## Active Object Pools (Sorted by Gets)\n"
    if top_pools.empty:
        md_content += "No data available.\n\n"
    else:
        md_content += "| Rank | Pool Type | Available | Max Capacity | Outstanding | Gets | Hit Rate | Status |\n"
        md_content += "| :---: | :--- | :---: | :---: | :---: | :---: | :---: | :---: |\n"
        for i, row in top_pools.iterrows():
            md_content += f"| {i+1} | `{row['pool_type']}` | {row['available']:,} | {row['max_capacity']:,} | {row['outstanding']:,} | {row['gets']:,} | {row['hit_rate']:.1f}% | {row['status']} |\n"
        md_content += "\n"
        
    # Anomalies
    md_content += "## Incident Logs (Anomalies)\n"
    has_incident = False
    
    if 'TotalConnections' in df.columns:
        spikes = detect_spikes(df.set_index('Timestamp')['TotalConnections'])
        for s in spikes:
            md_content += f"- **[{s.timestamp}]** [{s.severity}] {s.message}\n"
            has_incident = True
            
    # Check leaks / starvation for MD
    if 'TotalLeaked' in df.columns:
        max_leaked = df['TotalLeaked'].max()
        if max_leaked > 0:
            md_content += f"- **[CRITICAL]** Object Pool Leak detected! Max leaked objects: {max_leaked:.0f} objects. Verify checkout disposals.\n"
            has_incident = True
            
    if 'CacheHitRate' in df.columns:
        min_hit_rate = df['CacheHitRate'].min()
        if min_hit_rate < 90.0:
            md_content += f"- **[WARNING]** Object Pool Starvation Warning! Cache hit rate dropped to {min_hit_rate:.1f}%.\n"
            has_incident = True
            
    if not has_incident:
        md_content += "- No anomalies detected.\n"
        
    md_content += "\n"
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(md_content)
    print(f"Markdown Report generated at: {output_path}")

def main():
    parser_args = argparse.ArgumentParser(description="Nalix Telemetry Analyzer CLI")
    parser_args.add_argument("--dir", required=True, help="Directory containing CSV metrics")
    parser_args.add_argument("--format", choices=['html', 'md', 'all'], default='html', help="Output format")
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
    
    if args.format in ['html', 'all']:
        out_html = args.output if args.output.endswith('.html') else 'report.html'
        export_html(telemetry, out_html)
        
    if args.format in ['md', 'all']:
        out_md = args.output if args.output.endswith('.md') else 'report.md'
        export_md(telemetry, out_md)

if __name__ == "__main__":
    main()
