import streamlit as st
import pandas as pd
import plotly.express as px
import plotly.graph_objects as plotly_go
from plotly.subplots import make_subplots
import os

from src.parser import TelemetryParser, SchemaError
from src.analyzers import detect_spikes, detect_leak

st.set_page_config(page_title="Nalix Analyzer", layout="wide", page_icon="📈")

st.sidebar.header("Configuration")
metrics_dir = st.sidebar.text_input("Metrics Directory Path", value="../all_metrics_csv_20260526_214048")
z_threshold = st.sidebar.slider("Z-Score Spike Threshold", min_value=1.0, max_value=10.0, value=3.0, step=0.5)
top_n = st.sidebar.slider("Top N Endpoints", min_value=5, max_value=50, value=10, step=5)

if st.sidebar.button("Load Data"):
    if not os.path.exists(metrics_dir):
        st.sidebar.error("Directory not found!")
    else:
        with st.spinner("Loading and parsing telemetry..."):
            telemetry = TelemetryParser(metrics_dir)
            try:
                if telemetry.validate_and_load():
                    telemetry.process(top_n=top_n)
                    st.session_state['telemetry'] = telemetry
                    st.sidebar.success("Data loaded successfully!")
                else:
                    st.sidebar.warning("No data found.")
            except SchemaError as e:
                st.sidebar.error(str(e))

if 'telemetry' in st.session_state:
    telemetry = st.session_state['telemetry']
    df = telemetry.get_merged_data()
    top_endpoints = telemetry.get_top_endpoints()
    top_pools = telemetry.get_top_pools()
    
    st.title("Nalix Performance Dashboard")
    
    tab1, tab2, tab3, tab4, tab5 = st.tabs(["Overview", "Resource Health", "Network & Dispatch", "Object Pools", "Anomaly Logs"])
    
    with tab1:
        st.header("System Overview")
        if not df.empty:
            col1, col2, col3, col4 = st.columns(4)
            duration = df['Timestamp'].max() - df['Timestamp'].min()
            col1.metric("Recording Duration", str(duration).split('.')[0])
            
            if 'TotalConnections' in df.columns:
                peak_conn = df['TotalConnections'].max()
                col2.metric("Peak Connections", f"{peak_conn:,}")
                
            if 'ManagedHeapMB' in df.columns:
                avg_heap = df['ManagedHeapMB'].mean()
                col3.metric("Avg Managed Heap", f"{avg_heap:.1f} MB")
                
            if 'Threads' in df.columns:
                peak_threads = df['Threads'].max()
                col4.metric("Peak Threads", peak_threads)
                
            st.subheader("High-level Timeline")
            if 'TotalConnections' in df.columns:
                fig = px.line(df, x='Timestamp', y='TotalConnections', template="plotly_dark")
                st.plotly_chart(fig, use_container_width=True)

    with tab2:
        st.header("Resource Health")
        if 'WorkingSetMB' in df.columns and 'ManagedHeapMB' in df.columns:
            st.subheader("Memory Usage")
            fig = plotly_go.Figure()
            fig.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['WorkingSetMB'], name="Working Set", fill='tozeroy'))
            fig.add_trace(plotly_go.Scatter(x=df['Timestamp'], y=df['ManagedHeapMB'], name="Managed Heap"))
            fig.update_layout(template="plotly_dark")
            st.plotly_chart(fig, use_container_width=True)
            
            leak_res = detect_leak(df.set_index('Timestamp')['ManagedHeapMB'])
            if leak_res.is_leak:
                st.error(leak_res.message)
            else:
                st.success(leak_res.message)
                
        if 'Threads' in df.columns and 'ThreadsRunning' in df.columns:
            st.subheader("Thread Usage")
            fig = px.line(df, x='Timestamp', y=['Threads', 'ThreadsRunning'], template="plotly_dark")
            st.plotly_chart(fig, use_container_width=True)

    with tab3:
        st.header("Network & Dispatch")
        if not top_endpoints.empty:
            st.subheader("Top Congested Endpoints")
            st.dataframe(top_endpoints, use_container_width=True)
            
            fig = px.bar(top_endpoints, x='cumulative_pending', y='endpoint', orientation='h', 
                         title="Cumulative Pending Queues by Endpoint", template="plotly_dark")
            fig.update_layout(yaxis={'categoryorder':'total ascending'})
            st.plotly_chart(fig, use_container_width=True)

    with tab4:
        st.header("Object Pool Diagnostics")
        if not top_pools.empty:
            st.subheader("Active Object Pools (Sorted by Activity)")
            st.dataframe(top_pools, use_container_width=True)
            
            fig = px.bar(top_pools, x='gets', y='pool_type', orientation='h',
                         title="Pool Checkout Operations (Gets) by Type", template="plotly_dark")
            fig.update_layout(yaxis={'categoryorder':'total ascending'})
            st.plotly_chart(fig, use_container_width=True)
        else:
            st.info("No active pool data available in this session.")

    with tab5:
        st.header("Anomaly Logs")
        
        # 1. Connection Spikes
        if 'TotalConnections' in df.columns:
            st.subheader("Connection Spikes")
            spikes = detect_spikes(df.set_index('Timestamp')['TotalConnections'], z_threshold=z_threshold)
            if spikes:
                for s in spikes:
                    if s.severity == "CRITICAL":
                        st.error(f"**{s.timestamp}**: {s.message}")
                    else:
                        st.warning(f"**{s.timestamp}**: {s.message}")
            else:
                st.info("No connection spikes detected based on current threshold.")
                
        # 2. Object Pool Leaks & Starvation
        st.subheader("Object Pool Alerts")
        has_pool_alert = False
        
        if 'TotalLeaked' in df.columns:
            max_leaked = df['TotalLeaked'].max()
            if max_leaked > 0:
                st.error(f"**CRITICAL**: Object Pool Leak detected! Max leaked objects count: {max_leaked:.0f} objects. Verify checkout disposals.")
                has_pool_alert = True
                
        if 'CacheHitRate' in df.columns:
            min_hit_rate = df['CacheHitRate'].min()
            if min_hit_rate < 90.0:
                st.warning(f"**WARNING**: Object Pool Starvation Warning! Cache hit rate dropped to {min_hit_rate:.1f}%. Consider increasing DefaultMaxPoolSize.")
                has_pool_alert = True
                
        if not has_pool_alert:
            st.success("No Object Pool issues detected.")
else:
    st.info("Please load a metrics directory from the sidebar.")
