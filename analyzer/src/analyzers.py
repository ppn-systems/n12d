import numpy as np
import pandas as pd
from scipy import stats
import json
import re

class AttackWindow:
    def __init__(self, start_time, end_time, peak_connections, duration_seconds):
        self.start_time = start_time
        self.end_time = end_time
        self.peak_connections = peak_connections
        self.duration_seconds = duration_seconds

class AnomalyEvent:
    def __init__(self, timestamp, severity, message, value, expected_value, algorithm):
        self.timestamp = timestamp
        self.severity = severity
        self.message = message
        self.value = value
        self.expected_value = expected_value
        self.algorithm = algorithm

    def to_dict(self):
        return {
            'timestamp': self.timestamp,
            'severity': self.severity,
            'message': self.message,
            'value': self.value,
            'expected_value': self.expected_value,
            'algorithm': self.algorithm
        }

def detect_spikes(series: pd.Series, window: int = 30, z_threshold: float = 3.0) -> list[AnomalyEvent]:
    """
    Detect spikes using Rolling Z-Score.
    series: Series with DatetimeIndex
    """
    events = []
    if series is None or series.empty:
        return events
        
    # Shift series by 1 so the current value doesn't pollute the baseline rolling stats
    shifted_series = series.shift(1).fillna(series)
    rolling_mean = shifted_series.rolling(window=window, min_periods=1).mean()
    rolling_std = shifted_series.rolling(window=window, min_periods=1).std().fillna(0)
    
    for timestamp, val in series.items():
        mu = rolling_mean.loc[timestamp]
        sigma = rolling_std.loc[timestamp]
        
        # Edge case handling: sigma = 0
        if sigma < 1e-9:
            z_score = 99.0 if abs(val - mu) > 1e-9 else 0.0
        else:
            z_score = (val - mu) / sigma
            
        if abs(z_score) > z_threshold:
            severity = "CRITICAL" if abs(z_score) > z_threshold * 2 else "WARNING"
            events.append(AnomalyEvent(
                timestamp=timestamp,
                severity=severity,
                message=f"Spike detected in {series.name}: Value {val:.2f} (Z-score: {z_score:.2f})",
                value=val,
                expected_value=mu,
                algorithm="Rolling Z-Score"
            ))
            
    # Filter consecutive events to reduce noise
    filtered_events = []
    last_time = None
    for ev in events:
        if last_time is None or (ev.timestamp - last_time).total_seconds() > window:
            filtered_events.append(ev)
            last_time = ev.timestamp
            
    return filtered_events

class LeakResult:
    def __init__(self, is_leak, slope, p_value, message):
        self.is_leak = is_leak
        self.slope = slope
        self.p_value = p_value
        self.message = message

def detect_leak(series: pd.Series, slope_threshold: float = 0.05, p_threshold: float = 0.05) -> LeakResult:
    """
    Detect linear trend using OLS.
    """
    if len(series) < 2:
        return LeakResult(False, 0, 1, "Not enough data")
        
    # X in seconds from start
    x = (series.index - series.index[0]).total_seconds().values
    y = series.values
    
    # Check if x values vary (they should)
    if np.ptp(x) == 0:
        return LeakResult(False, 0, 1, "Invalid time range")
        
    res = stats.linregress(x, y)
    slope = res.slope
    p_value = res.pvalue
    
    is_leak = (slope > slope_threshold) and (p_value < p_threshold)
    message = f"Leak detected: Slope = {slope:.3f} unit/sec, p-value = {p_value:.4f}" if is_leak else f"No leak detected (Slope={slope:.3f}, p={p_value:.4f})"
    
    return LeakResult(is_leak, slope, p_value, message)

def compute_correlation_matrix(df: pd.DataFrame) -> pd.DataFrame:
    """
    Compute Pearson correlation matrix of numeric columns.
    """
    numeric_df = df.select_dtypes(include=[np.number])
    return numeric_df.corr(method='pearson')

def parse_json_safely(x):
    if pd.isna(x): return []
    if isinstance(x, str):
        try:
            return json.loads(x)
        except json.JSONDecodeError:
            return []
    return x

def compute_top_endpoints(df: pd.DataFrame, top_n: int = 10) -> pd.DataFrame:
    """
    Extract top N congested endpoints from PendingByConnection JSON column.
    Expected JSON format: [{"EndPoint": "ip:port", "Pending": count}]
    """
    if 'PendingByConnection' not in df.columns or 'Timestamp' not in df.columns:
        return pd.DataFrame(columns=['endpoint', 'cumulative_pending', 'peak_pending', 'peak_at'])
        
    all_records = []
    
    for idx, row in df.iterrows():
        ts = row['Timestamp']
        json_val = row['PendingByConnection']
        parsed = parse_json_safely(json_val)
        
        for item in parsed:
            endpoint = item.get('EndPoint', 'Unknown')
            pending = item.get('Pending', 0)
            if pending > 0:
                all_records.append({
                    'timestamp': ts,
                    'endpoint': endpoint,
                    'pending': pending
                })
                
    if not all_records:
        return pd.DataFrame(columns=['endpoint', 'cumulative_pending', 'peak_pending', 'peak_at'])
        
    records_df = pd.DataFrame(all_records)
    
    # Aggregate
    agg_df = records_df.groupby('endpoint').agg(
        cumulative_pending=('pending', 'sum'),
        peak_pending=('pending', 'max')
    ).reset_index()
    
    # Find timestamp of peak for each endpoint
    peak_times = records_df.loc[records_df.groupby('endpoint')['pending'].idxmax()][['endpoint', 'timestamp']]
    agg_df = agg_df.merge(peak_times, on='endpoint', how='left')
    agg_df.rename(columns={'timestamp': 'peak_at'}, inplace=True)
    
    agg_df = agg_df.sort_values('cumulative_pending', ascending=False).head(top_n)
    
    return agg_df

def compute_top_pools(df: pd.DataFrame, top_n: int = 10) -> pd.DataFrame:
    """
    Parse Pools from object_pools_metrics.csv last row and return active pools with calculated health indices.
    """
    if 'Pools' not in df.columns or df.empty:
        return pd.DataFrame(columns=['pool_type', 'available', 'max_capacity', 'outstanding', 'gets', 'hit_rate', 'status', 'utilization_pct', 'outstanding_pct', 'health_reason', 'recommendation'])
        
    last_row = df.iloc[-1]
    pools_json = last_row['Pools']
    parsed = parse_json_safely(pools_json)
    
    records = []
    for item in parsed:
        type_str = item.get('Type', 'Unknown')
        
        # Extract generic arguments if any
        generic_args = []
        matches = re.findall(r'\[\[(.*?)\]\]', type_str)
        for inner in matches:
            first_part = inner.split(',')[0]
            generic_args.append(first_part.split('.')[-1])
            
        base_type = type_str.split('`')[0].split('[')[0]
        base_name = base_type.split('.')[-1].replace('+', '.')
        
        if generic_args:
            type_name = f"{base_name}&lt;{', '.join(generic_args)}&gt;"
        else:
            type_name = base_name
            
        available = item.get('Available', 0)
        max_capacity = item.get('MaxCapacity', 0)
        outstanding = item.get('Outstanding', 0)
        hit_rate = item.get('HitRate', 100.0)
        gets = item.get('Gets', 0)
        consec_failures = item.get('ConsecutiveFailures', 0)
        
        # Calculate rates
        utilization_pct = ((max_capacity - available) / max_capacity * 100) if max_capacity > 0 else 0.0
        outstanding_pct = (outstanding / max_capacity * 100) if max_capacity > 0 else 0.0
        
        # Custom health threshold logic
        hit_status = "Healthy" if hit_rate >= 98.0 else ("Warning" if hit_rate >= 90.0 else "Critical")
        util_status = "Healthy" if utilization_pct < 70.0 else ("Warning" if utilization_pct <= 90.0 else "Critical")
        out_status = "Healthy" if outstanding_pct < 50.0 else ("Warning" if outstanding_pct <= 80.0 else "Critical")
        
        # Overall status determination
        if hit_status == "Critical" or util_status == "Critical" or out_status == "Critical" or consec_failures > 0:
            status = "Critical"
        elif hit_status == "Warning" or util_status == "Warning" or out_status == "Warning":
            status = "Warning"
        else:
            status = "OK"
            
        # Explanations and recommendations
        if hit_status == "Critical":
            reason = f"Low cache hit rate ({hit_rate:.1f}%) indicates extreme allocation pressure."
            recom = "Increase pool size or optimize object lifecycle to reduce GC pressure."
        elif util_status == "Critical" or out_status == "Critical":
            reason = f"Critical pool depletion: utilization is {utilization_pct:.1f}% (outstanding: {outstanding_pct:.1f}%)."
            recom = f"Increase pool size capacity or check for object leaks in lease lifetime."
        elif consec_failures > 0:
            reason = f"Allocation failures detected: {consec_failures} consecutive failures."
            recom = "Verify objects are correctly returned to the pool and check for sudden thread surges."
        elif hit_status == "Warning":
            reason = f"Cache hit rate is slightly low ({hit_rate:.1f}%). Minor allocation pressure."
            recom = "Monitor pool hits; consider slightly increasing capacity if traffic scales."
        elif util_status == "Warning" or out_status == "Warning":
            reason = f"Pool under heavy load: utilization is {utilization_pct:.1f}%."
            recom = "Pool is operating close to its limit. Monitor for starvation under burst traffic."
        else:
            reason = "Pool is operating healthy with stable utilization and high cache hit rate."
            recom = "No action required."
            
        records.append({
            'pool_type': type_name,
            'available': available,
            'max_capacity': max_capacity,
            'outstanding': outstanding,
            'gets': gets,
            'hit_rate': hit_rate,
            'status': status,
            'utilization_pct': utilization_pct,
            'outstanding_pct': outstanding_pct,
            'health_reason': reason,
            'recommendation': recom
        })
        
    pools_df = pd.DataFrame(records)
    if not pools_df.empty:
        # Sort Critical/Warning first, then by activity (gets)
        status_rank = {'Critical': 0, 'Warning': 1, 'OK': 2}
        pools_df['rank'] = pools_df['status'].map(status_rank)
        pools_df = pools_df.sort_values(by=['rank', 'gets'], ascending=[True, False]).drop(columns=['rank']).head(top_n)
        
    return pools_df

def compute_pool_timeline(df: pd.DataFrame) -> pd.DataFrame:
    """
    Parse Pools JSON at each row to compile a history of pools over time.
    """
    if 'Pools' not in df.columns or df.empty:
        return pd.DataFrame()
        
    records = []
    for idx, row in df.iterrows():
        ts = row['Timestamp']
        pools_json = row['Pools']
        parsed = parse_json_safely(pools_json)
        for item in parsed:
            type_str = item.get('Type', 'Unknown')
            
            # Clean type name
            generic_args = []
            matches = re.findall(r'\[\[(.*?)\]\]', type_str)
            for inner in matches:
                first_part = inner.split(',')[0]
                generic_args.append(first_part.split('.')[-1])
                
            base_type = type_str.split('`')[0].split('[')[0]
            base_name = base_type.split('.')[-1].replace('+', '.')
            
            if generic_args:
                type_name = f"{base_name}&lt;{', '.join(generic_args)}&gt;"
            else:
                type_name = base_name
                
            records.append({
                'Timestamp': ts,
                'PoolType': type_name,
                'Available': item.get('Available', 0),
                'MaxCapacity': item.get('MaxCapacity', 0),
                'Outstanding': item.get('Outstanding', 0),
                'Gets': item.get('Gets', 0),
                'Hits': item.get('Hits', 0),
                'Misses': item.get('Misses', 0),
                'HitRate': item.get('HitRate', 100.0),
                'Status': item.get('Status', 'OK')
            })
            
    return pd.DataFrame(records)

def compute_buffer_breakdown(df: pd.DataFrame) -> pd.DataFrame:
    """
    Parse Pools from buffers_metrics.csv at each row to compile timeline metrics per bucket size.
    """
    if 'Pools' not in df.columns or df.empty:
        return pd.DataFrame()
        
    records = []
    for idx, row in df.iterrows():
        ts = row['Timestamp']
        pools_json = row['Pools']
        parsed = parse_json_safely(pools_json)
        for item in parsed:
            size = item.get('BufferSize', 0)
            if size >= 1024 * 1024:
                size_label = f"{size / (1024*1024):.0f}MB"
            elif size >= 1024:
                size_label = f"{size / 1024:.0f}KB"
            else:
                size_label = f"{size}B"
                
            records.append({
                'Timestamp': ts,
                'BufferSize': size,
                'SizeLabel': size_label,
                'InUse': item.get('InUse', 0),
                'Total': item.get('Total', 0),
                'Free': item.get('Free', 0),
                'Hits': item.get('Hits', 0),
                'Expands': item.get('Expands', 0),
                'Shrinks': item.get('Shrinks', 0),
                'UsageRatio': item.get('UsageRatio', 0.0)
            })
            
    return pd.DataFrame(records)

def compute_instance_timeline(df: pd.DataFrame) -> pd.DataFrame:
    """
    Cleans up and processes instances metrics timeline data.
    """
    if df.empty:
        return pd.DataFrame()
        
    res_df = df.copy()
    if 'HitRatePermille' in res_df.columns:
        res_df['HitRate'] = res_df['HitRatePermille'] / 10.0
    else:
        res_df['HitRate'] = 100.0
        
    if 'InstanceCacheHitCount' in res_df.columns and 'InstanceCreationCount' in res_df.columns:
        total = res_df['InstanceCacheHitCount'] + res_df['InstanceCreationCount']
        res_df['CacheEfficiency'] = np.where(total > 0, (res_df['InstanceCacheHitCount'] / total) * 100, 100.0)
    else:
        res_df['CacheEfficiency'] = 100.0
        
    return res_df

def compute_cached_instances(df: pd.DataFrame) -> pd.DataFrame:
    """
    Parse latest Instances list from the instances_metrics.csv last row.
    """
    if 'Instances' not in df.columns or df.empty:
        return pd.DataFrame(columns=['instance_type', 'is_disposable', 'source'])
        
    last_row = df.iloc[-1]
    instances_json = last_row['Instances']
    parsed = parse_json_safely(instances_json)
    
    records = []
    for item in parsed:
        type_str = item.get('Type', 'Unknown')
        
        # Clean type name
        generic_args = []
        matches = re.findall(r'\[\[(.*?)\]\]', type_str)
        for inner in matches:
            first_part = inner.split(',')[0]
            generic_args.append(first_part.split('.')[-1])
            
        base_type = type_str.split('`')[0].split('[')[0]
        base_name = base_type.split('.')[-1].replace('+', '.')
        
        if generic_args:
            type_name = f"{base_name}&lt;{', '.join(generic_args)}&gt;"
        else:
            type_name = base_name
            
        records.append({
            'instance_type': type_name,
            'is_disposable': 'Yes' if item.get('IsDisposable', False) else 'No',
            'source': item.get('Source', 'Unknown')
        })
        
    return pd.DataFrame(records)


def detect_attack_window(connections_series: pd.Series) -> AttackWindow:
    if connections_series is None or connections_series.empty or len(connections_series) < 5:
        return None
        
    diff = connections_series.diff()
    # Simple heuristic: look for massive spikes (e.g. > 100 connections jump)
    threshold = 100
    spikes = diff[diff > threshold]
    drops = diff[diff < -threshold]
    
    if spikes.empty:
        return None
        
    start_time = spikes.index[0]
    # Find the drop that corresponds to the end of the attack (after the start)
    valid_drops = drops[drops.index > start_time]
    end_time = valid_drops.index[0] if not valid_drops.empty else connections_series.index[-1]
    
    peak_connections = connections_series.loc[start_time:end_time].max()
    duration_seconds = (end_time - start_time).total_seconds()
    
    return AttackWindow(start_time, end_time, peak_connections, duration_seconds)

def compute_endpoints(json_series: pd.Series, attack_window: AttackWindow = None) -> pd.DataFrame:
    all_records = []
    
    for ts, json_val in json_series.items():
        parsed = parse_json_safely(json_val)
        for item in parsed:
            addr = item.get('Address', 'Unknown')
            if addr == '::1':
                continue # Skip localhost
            
            risk = "Low"
            if attack_window and attack_window.start_time <= ts <= attack_window.end_time:
                risk = "Suspected Attacker"
            elif item.get('TotalConnectionsToday', 0) > 100:
                risk = "High"
                
            all_records.append({
                'Address': addr,
                'CurrentConnections': item.get('CurrentConnections', 0),
                'TotalConnectionsToday': item.get('TotalConnectionsToday', 0),
                'Risk': risk,
                'Timestamp': ts
            })
            
    if not all_records:
        return pd.DataFrame(columns=['Address', 'CurrentConnections', 'TotalConnectionsToday', 'Risk'])
        
    df = pd.DataFrame(all_records)
    # Get latest/max state per address
    df_agg = df.groupby('Address').agg(
        CurrentConnections=('CurrentConnections', 'max'),
        TotalConnectionsToday=('TotalConnectionsToday', 'max'),
        Risk=('Risk', 'last')
    ).reset_index()
    
    return df_agg.sort_values('CurrentConnections', ascending=False)

def detect_worker_imbalance(json_series: pd.Series) -> list[AnomalyEvent]:
    events = []
    if json_series is None or json_series.empty:
        return events
        
    # Analyze the latest snapshot or look for persistent imbalance
    latest_ts = json_series.index[-1]
    parsed = parse_json_safely(json_series.iloc[-1])
    
    if not parsed:
        return events
        
    loads = []
    for w in parsed:
        name = w.get('Name', '')
        # The JSON uses 'Progress' for iterations/runs
        runs = w.get('Progress', 0)
        loads.append({'Name': name, 'Runs': runs})
        
    if len(loads) < 2:
        return events
        
    runs_array = np.array([l['Runs'] for l in loads if l['Runs'] > 0])
    if len(runs_array) < 2:
        return events
        
    max_load = runs_array.max()
    min_load = runs_array.min()
    
    if max_load > 0 and (max_load - min_load) / max_load > 0.5: # 50% variance
        events.append(AnomalyEvent(
            timestamp=latest_ts,
            severity="WARNING",
            message=f"Worker imbalance detected! Max runs: {max_load}, Min runs: {min_load}.",
            value=(max_load - min_load) / max_load,
            expected_value=0.0,
            algorithm="Load Variance"
        ))
        
    return events

def compute_task_details(recurring_series: pd.Series, workers_series: pd.Series):
    rec_df = pd.DataFrame(columns=['Task Name', 'Interval', 'Executions', 'Status'])
    work_df = pd.DataFrame(columns=['Worker', 'TotalRuns', 'IsRunning', 'LastRun'])
    
    if not recurring_series.empty:
        parsed = parse_json_safely(recurring_series.iloc[-1])
        records = []
        for p in parsed:
            records.append({
                'Task Name': p.get('Name', 'Unknown'),
                'Executions': p.get('TotalRuns', 0),
                'Status': 'Running' if p.get('IsRunning', False) else 'Idle'
            })
        rec_df = pd.DataFrame(records)
        
    if not workers_series.empty:
        parsed = parse_json_safely(workers_series.iloc[-1])
        records = []
        for p in parsed:
            records.append({
                'Worker': p.get('Name', 'Unknown'),
                'Progress': p.get('Progress', 0),
                'IsRunning': 'Running',
                'LastRun': p.get('LastHeartbeatUtc', '')
            })
        work_df = pd.DataFrame(records)
        
    return rec_df, work_df
