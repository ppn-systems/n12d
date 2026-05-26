import numpy as np
import pandas as pd
from scipy import stats
import json

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
    Parse Pools from object_pools_metrics.csv last row and return active pools sorted by activity (gets).
    """
    if 'Pools' not in df.columns or df.empty:
        return pd.DataFrame(columns=['pool_type', 'available', 'max_capacity', 'outstanding', 'gets', 'hit_rate', 'status'])
        
    last_row = df.iloc[-1]
    pools_json = last_row['Pools']
    parsed = parse_json_safely(pools_json)
    
    records = []
    for item in parsed:
        type_str = item.get('Type', 'Unknown')
        # Simplify generics and namespace
        if '`' in type_str:
            type_str = type_str.split('`')[0]
        elif '[' in type_str:
            type_str = type_str.split('[')[0]
        type_name = type_str.split('.')[-1]
        
        records.append({
            'pool_type': type_name,
            'available': item.get('Available', 0),
            'max_capacity': item.get('MaxCapacity', 0),
            'outstanding': item.get('Outstanding', 0),
            'gets': item.get('Gets', 0),
            'hit_rate': item.get('HitRate', 0.0),
            'status': item.get('Status', 'OK')
        })
        
    pools_df = pd.DataFrame(records)
    if not pools_df.empty:
        pools_df = pools_df.sort_values('gets', ascending=False).head(top_n)
        
    return pools_df
