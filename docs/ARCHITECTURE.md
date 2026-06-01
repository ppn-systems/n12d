# 📐 System Architecture: Nalix Observability Ecosystem

This document describes the high-level system design, communication protocols, mathematical engines, and components comprising the **Nalix Observability** telemetry system.

---

## 1. Architectural Overview

The Nalix Observability ecosystem is designed to capture, stream, visualize, and mathematically analyze runtime telemetry data from high-performance Nalix applications. It operates in two phases:
1. **Real-time Phase:** Active telemetry collection and interactive streaming via WebSockets.
2. **Post-mortem Phase:** Offline mathematical diagnostic analysis of historical log CSV exports.

### System Components & Data Flow

```mermaid
graph TD
    subgraph "Nalix Application Server (C#)"
        Core["Nalix Core Applications<br/>(Nalix.SDK / Nalix.Framework)"]
        Handlers["Nalix.Observability.Handlers<br/>(Access & Runtime Observation Handlers)"]
        Contracts["Nalix.Observability.Contracts<br/>(Telemetry Packets & Data Contracts)"]
    end

    subgraph "Real-Time UI (Blazor WASM)"
        Dashboard["Nalix.Dashboard<br/>(Blazor WebAssembly App)"]
    end

    subgraph "Offline Post-Mortem Analytics (Python)"
        CSVs[("Exported Telemetry CSVs<br/>(Object Pools, Threads, Dispatch, etc.)")]
        Analyzer["Nalix.Analyzer<br/>(Diagnostics Engine: CLI & Streamlit)"]
    end

    %% Interactions
    Core ----> Handlers
    Handlers -->|Reference| Contracts
    Dashboard -->|1. WebSocket Connection & Auth<br/>(64-char Security Key)| Handlers
    Handlers -->|2. Real-Time Telemetry Stream| Dashboard
    Dashboard -->|3. Manual CSV Export| CSVs
    CSVs -->|4. Ingestion & Alignment| Analyzer
```

---

## 2. Telemetry Ingestion & Real-time Streaming

### Instrumentation Handlers
The telemetry pipeline is hosted inside the backend application via C# handlers registered in the Nalix `NetworkApplication` hosting builder:
* **`ObservabilityAccessHandlers`**: Handles connection authentication, connection authorization, and secure handshakes. Access is guarded by a **64-character hexadecimal security key** provided during initialization.
* **`RuntimeObservationHandlers`**: Pulls system statistics from the .NET Runtime and Nalix framework pools at configured intervals, formatting them into telemetry packets.

### WebSocket Communication Contract
Real-time data streaming is built on raw WebSockets (`ws://<host>:<port>/ws/`) implementing a customized protocol specified by `DefaultProtocol`. 
* **Handshake Protocol:** The client (Blazor WebAssembly) initiates connection, sends the 64-character verification key.
* **Streaming Protocol:** Once authorized, the backend pushes serialized telemetry packets at regular frequencies. This contains data sections for:
  - System performance (CPU, WorkingSetMB, Threads, ThreadsRunning, ManagedHeapMB).
  - Dispatch Metrics (`PendingByConnection` queues).
  - Object Pool instances and allocation arities.
  - Active buffers, tasks, and historical session recordings.

---

## 3. Post-Mortem Diagnostics Engine (Nalix Analyzer)

When debugging performance regressions (e.g., slow connections, memory leaks, thread starvation) that occurred in production, offline telemetry logs can be ingested by the Python-based diagnostics engine. 

### A. Time-Series Ingestion & Chronological Alignment
Because different telemetry files (e.g. `process_metrics.csv`, `dispatch_metrics.csv`, `object_pools_metrics.csv`) are written asynchronously by the Dashboard exporter, time steps may not match precisely. 
The analyzer resolves this by:
1. Validating expected schemas across CSV sources.
2. Normalizing columns (stripping system-specific prefixes, e.g. `Process.Threads` -> `Threads`).
3. Executing a **Pandas time-based merge** using `pd.merge_asof` matching on timestamps to align all metric series onto a synchronized timeline.

### B. Mathematical Detection Algorithms

The analyzer uses statistical algorithms rather than hardcoded thresholds to avoid false positives:

#### 1. Connection Spike Detection (Rolling Z-Score)
To detect sudden traffic or connection anomalies:
$$\text{Rolling Mean } \mu_t = \text{mean}(x_{t-W}, \dots, x_{t-1})$$
$$\text{Rolling Std Dev } \sigma_t = \text{std}(x_{t-W}, \dots, x_{t-1})$$
$$\text{Z-Score } Z_t = \frac{x_t - \mu_t}{\sigma_t}$$

* **Mandatory Shifted Window:** The rolling window must exclude the current value $x_t$ (by applying `.shift(1)` in Pandas). Otherwise, a massive spike at $x_t$ would inflate $\sigma_t$ and deflate the resulting Z-score, masking the anomaly.
* **Zero Standard Deviation Safeguard:** In highly stable states where $\sigma_t = 0$:
  - If $x_t \neq \mu_t$, a default anomaly score of $Z = 99.0$ is assigned.
  - If $x_t = \mu_t$, $Z = 0.0$.

#### 2. Resource Leak Detection (Ordinary Least Squares Linear Regression)
To identify slow leaks (RAM working sets, thread handles):
$$\hat{y} = \beta_0 + \beta_1 x$$

* **Trend Significance (`p-value`):** A positive slope $\beta_1 > 0$ does not automatically signify a leak. The analyzer validates the trend by calculating the statistical significance `p-value` using `scipy.stats.linregress`. A trend is flagged as a leak **only if $p\text{-value} < 0.05$**, which prevents random noise or short durations from triggering false warnings.

---

## 4. Object pool Clean-up & Generic Arity Stripping
To ensure telemetry metrics are easy to read, the system cleans up generic C# arity formats exported from type names:
* Generic types serialized as `PacketContext`1` or `ObjectMap`2[[...]]` are normalized by stripping arity backticks (`'``'`) to map cleanly to `PacketContext` or `ObjectMap`.
