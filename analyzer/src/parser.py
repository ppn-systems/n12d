import os
import pandas as pd
import glob
from .analyzers import compute_top_endpoints, compute_top_pools

class SchemaError(Exception):
    pass

COLUMN_RENAME_MAP = {
    'Process.Threads': 'Threads',
    'Process.ThreadsRunning': 'ThreadsRunning',
    'Memory.WorkingSetMB': 'WorkingSetMB',
    'Process.ManagedHeapMB': 'ManagedHeapMB',
}

FILE_SCHEMA_MAP = {
    'connections_metrics.csv': ['Timestamp', 'TotalConnections'],
    'dispatch_metrics.csv': ['Timestamp', 'PendingPerPriority.HIGH', 'PendingPerPriority.URGENT', 'PendingByConnection'],
    'tasks_metrics.csv': [
        'Timestamp', 
        'AverageWorkerExecutionTimeMs', 
        'Process.Threads', 
        'Process.ThreadsRunning',
        'Memory.WorkingSetMB',
        'Process.ManagedHeapMB'
    ],
    'object_pools_metrics.csv': ['Timestamp', 'Pools', 'TotalLeaked', 'CacheHitRate'],
    'buffers_metrics.csv': ['Timestamp'],
    'instances_metrics.csv': ['Timestamp'],
    'listener_metrics.csv': ['Timestamp'],
    'sessions_metrics.csv': ['Timestamp'],
    'protocol_metrics.csv': ['Timestamp'],
}

class TelemetryParser:
    def __init__(self, metrics_dir: str):
        self.metrics_dir = metrics_dir
        self.raw_dfs = {}
        self.merged_df = pd.DataFrame()
        self.top_endpoints_df = pd.DataFrame()
        self.top_pools_df = pd.DataFrame()

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
                    
            # Rename columns to standard names
            df.rename(columns=COLUMN_RENAME_MAP, inplace=True)
            
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
            
        # Extract endpoints from whichever file contains PendingByConnection
        dispatch_file = next((f for f in self.raw_dfs.keys() if 'dispatch' in f), None)
        if dispatch_file and 'PendingByConnection' in self.raw_dfs[dispatch_file].columns:
            self.top_endpoints_df = compute_top_endpoints(self.raw_dfs[dispatch_file], top_n=top_n)
            
        # Extract active object pools from object pools metrics
        pools_file = next((f for f in self.raw_dfs.keys() if 'object_pools' in f), None)
        if pools_file and 'Pools' in self.raw_dfs[pools_file].columns:
            self.top_pools_df = compute_top_pools(self.raw_dfs[pools_file], top_n=top_n)
            
        # Merge all dataframes on Timestamp using merge_asof
        # We start with the dataframe that has the most rows, or just use connections as base
        conn_file = next((f for f in self.raw_dfs.keys() if 'connections' in f), None)
        base_file = conn_file if conn_file else list(self.raw_dfs.keys())[0]
        
        self.merged_df = self.raw_dfs[base_file].copy()
        
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
