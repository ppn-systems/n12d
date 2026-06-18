import os
import pandas as pd
import glob
from .analyzers import (
    compute_top_endpoints, 
    compute_top_pools, 
    compute_pool_timeline, 
    compute_buffer_breakdown, 
    compute_instance_timeline
)

class SchemaError(Exception):
    pass

COLUMN_RENAME_MAP = {
    'Process.Threads': 'Threads',
    'Process.ThreadsRunning': 'ThreadsRunning',
    'Memory.WorkingSetMB': 'WorkingSetMB',
    'Process.ManagedHeapMB': 'ManagedHeapMB',
    'Memory.PrivateMB': 'PrivateMB',
    'Memory.VirtualMB': 'VirtualMB',
}

# Per-file column renames to avoid merge conflicts when multiple files share column names
FILE_COLUMN_RENAME_MAP = {
    'concurrency_gate_metrics.csv': {
        'RejectionRate': 'ConcurrencyGate.RejectionRate',
        'TotalRejected': 'ConcurrencyGate.TotalRejected',
        'TotalAcquired': 'ConcurrencyGate.TotalAcquired',
        'TotalQueued': 'ConcurrencyGate.TotalQueued',
        'TotalCleaned': 'ConcurrencyGate.TotalCleaned',
    },
    'sessions_metrics.csv': {
        'TotalStoresFailed': 'Sessions.TotalStoresFailed',
        'TotalStoresSucceeded': 'Sessions.TotalStoresSucceeded',
        'TotalStoresRejectedByPolicy': 'Sessions.TotalStoresRejectedByPolicy',
    },
}

FILE_SCHEMA_MAP = {
    'connections_metrics.csv': ['Timestamp', 'TotalConnections', 'IngressBytesPerSecond', 'EgressBytesPerSecond', 'TotalBytesReceived', 'TotalBytesSent'],
    'dispatch_metrics.csv': ['Timestamp', 'PendingPerPriority.HIGH', 'PendingPerPriority.URGENT', 'PendingPerPriority.LOW', 'PendingPerPriority.MEDIUM', 'PendingPerPriority.NONE', 'PendingByConnection', 'PipelineMetrics.ActiveExecutions', 'PipelineMetrics.AverageTimeMs', 'PipelineMetrics.TotalExecutions', 'PipelineMetrics.TotalErrors', 'TotalPackets', 'WakeReads', 'WakeSignals', 'ReadyConnections', 'Running'],
    'tasks_metrics.csv': [
        'Timestamp',
        'AverageWorkerUptimeMs',
        'Process.Threads',
        'Process.ThreadsRunning',
        'Memory.WorkingSetMB',
        'Process.ManagedHeapMB',
        'P95WorkerUptimeMs',
        'P99WorkerUptimeMs',
        'PeakRunningWorkerCount',
        'WorkersRunning',
        'WorkersTotal',
        'Process.GCGen0',
        'Process.GCGen1',
        'Process.GCGen2',
        'Memory.PrivateMB',
        'Recurring',
        'TopRunningWorkers',
        'Process.UptimeDays',
        'CurrentConcurrencyLimit',
        'WorkerCompletionCount',
        'WorkerErrorCount',
        'RecurringCount',
        'RecurringErrorCount',
        'RecurringExecutionCount',
    ],
    'object_pools_metrics.csv': ['Timestamp', 'Pools', 'TotalLeaked', 'CacheHitRate', 'TotalCreated', 'TotalDisposed', 'TotalGetOperations', 'TotalReturnOperations', 'PoolCount', 'UnhealthyPoolCount', 'PeakPoolCount', 'Throughput'],
    'buffers_metrics.csv': ['Timestamp', 'PeakMemoryUsageBytes', 'ThroughputMBps', 'HitRate', 'TotalExpands', 'TotalShrinks', 'Pools', 'PoolCount', 'TrimCycleCount', 'BucketCacheHits', 'BucketCacheMisses'],
    'instances_metrics.csv': ['Timestamp', 'CachedInstanceCount', 'HitRatePermille', 'InstanceCreationCount', 'TotalGetOrCreateCalls', 'Instances'],
    'listener_metrics.csv': ['Timestamp', 'TCP.Connections.ActiveConnections', 'TCP.Metrics.TotalAccepted', 'TCP.Metrics.TotalErrors', 'TCP.Metrics.TotalRejected', 'TCP.Metrics.AcceptQueueDepth', 'TCP.Metrics.LimiterRejections', 'TCP.Metrics.QueueFullRejections', 'TCP.Metrics.PendingProxyConnections', 'WEBSOCKET.Metrics.TotalAccepted', 'WEBSOCKET.Metrics.TotalErrors', 'WEBSOCKET.Metrics.TotalRejected', 'WEBSOCKET.Metrics.AcceptQueueDepth', 'WEBSOCKET.Metrics.LimiterRejections', 'WEBSOCKET.Metrics.QueueFullRejections'],
    'sessions_metrics.csv': ['Timestamp', 'Store.ActiveSessions', 'Store.TotalConsumed', 'Store.TotalExpired', 'Store.TotalStored', 'TotalConsumesAttempted', 'TotalConsumesFailed', 'TotalConsumesSucceeded', 'TotalStoresAttempted', 'TotalStoresFailed', 'TotalStoresSucceeded', 'TotalStoresRejectedByPolicy'],
    'protocol_metrics.csv': ['Timestamp', 'TCP.TotalMessages', 'TCP.TotalErrors', 'WEBSOCKET.TotalMessages', 'WEBSOCKET.TotalErrors'],
    'connection_guard_metrics.csv': ['Timestamp', 'TotalAttempts', 'TotalRejections', 'RejectionRate', 'TotalConcurrent', 'AdaptiveActive', 'EWMARate', 'TrackedEndpoints', 'MaxPerEndpoint', 'TotalCleaned', 'SubnetTrackedV4', 'SubnetTrackedV6'],
    'concurrency_gate_metrics.csv': ['Timestamp', 'CircuitBreaker.IsOpen', 'CircuitBreaker.Trips', 'TotalAcquired', 'TotalQueued', 'TotalRejected', 'RejectionRate', 'TotalCleaned'],
    'token_bucket_limiter_metrics.csv': ['Timestamp', 'Endpoints', 'HardBlockedCount', 'CapacityTokens', 'TrackedEndpoints', 'RefillPerSecond'],
    'policy_rate_limiter_metrics.csv': ['Timestamp', 'SharedEngine.Endpoints', 'SharedEngine.HardBlockedCount', 'SharedEngine.CapacityTokens', 'SharedEngine.TrackedEndpoints', 'SharedEngine.RefillPerSecond'],
}

class TelemetryParser:
    def __init__(self, metrics_dir: str):
        self.metrics_dir = metrics_dir
        self.raw_dfs = {}
        self.json_series = {} # Store JSON columns before merging out
        self.merged_df = pd.DataFrame()
        self.top_endpoints_df = pd.DataFrame()
        self.top_pools_df = pd.DataFrame()
        self.pool_timeline_df = pd.DataFrame()
        self.buffer_breakdown_df = pd.DataFrame()
        self.instance_timeline_df = pd.DataFrame()
        self.recurring_tasks_df = pd.DataFrame()
        self.top_workers_df = pd.DataFrame()
        self.endpoints_df = pd.DataFrame()


    def validate_and_load(self):
        """Loads all known CSVs and validates schema."""
        if not os.path.exists(self.metrics_dir):
            raise FileNotFoundError(f"Metrics directory not found: {self.metrics_dir}")
            
        csv_files = glob.glob(os.path.join(self.metrics_dir, "*.csv"))
        if not csv_files:
            raise FileNotFoundError(f"No CSV files found in {self.metrics_dir}")
            
        for file_path in csv_files:
            file_name = os.path.basename(file_path)
            
            # Read CSV
            try:
                df = pd.read_csv(file_path)
            except Exception as e:
                print(f"Warning: Could not read {file_name}: {e}")
                continue
                
            if df.empty:
                print(f"Warning: File {file_name} is empty. Skipping.")
                continue
                
            # Check schema if defined
            if file_name in FILE_SCHEMA_MAP:
                required_cols = FILE_SCHEMA_MAP[file_name]
                missing = [col for col in required_cols if col not in df.columns]
                if missing:
                    raise SchemaError(f"File {file_name} is missing required columns: {missing}")
                    
            # Rename columns to standard names (global + per-file)
            df.rename(columns=COLUMN_RENAME_MAP, inplace=True)
            if file_name in FILE_COLUMN_RENAME_MAP:
                df.rename(columns=FILE_COLUMN_RENAME_MAP[file_name], inplace=True)
            
            # Clean up numeric columns that might have comma decimal separators
            for col in df.columns:
                if col in ['Timestamp', 'UtcNow', 'StartTime', 'Pools', 'TopEndpoints', 'Recurring', 'TopRunningWorkers', 'PendingByConnection', 'Instances', 'UnhealthyPools', 'Endpoints', 'SharedEngine.Endpoints']:
                    continue
                if pd.api.types.is_string_dtype(df[col]) or df[col].dtype == 'object':
                    try:
                        converted = pd.to_numeric(df[col].astype(str).str.replace(',', '.'), errors='coerce')
                        if not converted.isna().all():
                            df[col] = converted
                    except Exception:
                        pass
            
            # Parse Timestamp to datetime
            if 'Timestamp' in df.columns:
                df['Timestamp'] = pd.to_datetime(df['Timestamp'])
                # Sort by timestamp to ensure merge_asof works
                df = df.sort_values('Timestamp')
            else:
                print(f"Warning: File {file_name} has no Timestamp column. Skipping.")
                continue
                
            self.raw_dfs[file_name] = df
            
        return len(self.raw_dfs) > 0

    def process(self, top_n: int = 10):
        """Aligns all dataframes into a single merged dataframe, computes top endpoints and active object pools."""
        if not self.raw_dfs:
            return
            
        # Store and remove JSON columns so they don't corrupt the numeric merge
        json_columns = ['Pools', 'TopEndpoints', 'Recurring', 'TopRunningWorkers', 'PendingByConnection', 'Instances', 'Endpoints', 'SharedEngine.Endpoints']
        
        for file_name, df in self.raw_dfs.items():
            for jc in json_columns:
                if jc in df.columns:
                    # Store as Series indexed by Timestamp
                    self.json_series[f"{file_name}_{jc}"] = df.set_index('Timestamp')[jc].dropna()
                    
        # Extract endpoints from whichever file contains PendingByConnection
        dispatch_file = next((f for f in self.raw_dfs.keys() if 'dispatch' in f), None)
        if dispatch_file and 'PendingByConnection' in self.raw_dfs[dispatch_file].columns:
            self.top_endpoints_df = compute_top_endpoints(self.raw_dfs[dispatch_file], top_n=top_n)
            
        # Extract active object pools from object pools metrics
        pools_file = next((f for f in self.raw_dfs.keys() if 'object_pools' in f), None)
        if pools_file and 'Pools' in self.raw_dfs[pools_file].columns:
            self.top_pools_df = compute_top_pools(self.raw_dfs[pools_file], top_n=top_n)
            self.pool_timeline_df = compute_pool_timeline(self.raw_dfs[pools_file])
            
        # Extract buffer breakdown if buffers file exists
        buffers_file = next((f for f in self.raw_dfs.keys() if 'buffers' in f), None)
        if buffers_file and 'Pools' in self.raw_dfs[buffers_file].columns:
            self.buffer_breakdown_df = compute_buffer_breakdown(self.raw_dfs[buffers_file])
            
        # Extract instance cache timeline if instances file exists
        instances_file = next((f for f in self.raw_dfs.keys() if 'instances' in f), None)
        if instances_file:
            self.instance_timeline_df = compute_instance_timeline(self.raw_dfs[instances_file])
            
        # Merge all dataframes on Timestamp using merge_asof
        conn_file = next((f for f in self.raw_dfs.keys() if 'connections' in f), None)
        base_file = conn_file if conn_file else list(self.raw_dfs.keys())[0]
        
        # Start with base_file, excluding JSON cols
        cols_base = [c for c in self.raw_dfs[base_file].columns if c not in json_columns]
        self.merged_df = self.raw_dfs[base_file][cols_base].copy()
        
        for file_name, df in self.raw_dfs.items():
            if file_name == base_file:
                continue
                
            # Keep only numeric columns + Timestamp to avoid merge conflicts with strings
            cols_to_merge = ['Timestamp'] + list(df.select_dtypes(include='number').columns)
            # Remove duplicated columns from df that already exist in merged_df (except Timestamp)
            cols_to_merge = [c for c in cols_to_merge if c == 'Timestamp' or c not in self.merged_df.columns]
            
            df_subset = df[cols_to_merge]
            
            # merge_asof requires both DataFrames to be sorted by the key
            self.merged_df = pd.merge_asof(
                self.merged_df, 
                df_subset, 
                on='Timestamp', 
                direction='nearest',
                tolerance=pd.Timedelta(seconds=5)
            )
            
        # Forward fill missing values
        self.merged_df.ffill(inplace=True)
        self.merged_df.fillna(0, inplace=True)
        
    def get_merged_data(self) -> pd.DataFrame:
        return self.merged_df
        
    def get_top_endpoints(self) -> pd.DataFrame:
        return self.top_endpoints_df
        
    def get_top_pools(self) -> pd.DataFrame:
        return self.top_pools_df

    def get_pool_timeline(self) -> pd.DataFrame:
        return self.pool_timeline_df

    def get_buffer_breakdown(self) -> pd.DataFrame:
        return self.buffer_breakdown_df

    def get_instance_timeline(self) -> pd.DataFrame:
        return self.instance_timeline_df

    def get_json_series(self, key_contains: str) -> pd.Series:
        for k, s in self.json_series.items():
            if key_contains in k:
                return s
        return pd.Series(dtype='object')
