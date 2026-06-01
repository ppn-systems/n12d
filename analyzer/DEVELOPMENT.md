# 📜 Nalix Telemetry Analyzer: Development Rules & Guidelines

This document records the **Data Contract design rules**, **project architecture**, and **mathematical edge cases** of the Nalix Diagnostics Engine. Any developer (or AI coding assistant) working on this project in the future must adhere to these rules to prevent regressions.

---

## 1. 📂 Standard Project Structure (Project Layout)

The project follows a standard structured Python package format:
* **Root Folder (`/analyzer`)**: Contains only entry point scripts (`app.py`, `cli.py`), build script (`build.bat`), dependencies (`requirements.txt`), and documentation (`README.md`, `DEVELOPMENT.md`).
* **Source Package (`/analyzer/src/`)**: Holds all core business and parsing logic:
  * `src/parser.py`: Schema validation, column normalization, and time alignment pipeline.
  * `src/analyzers.py`: Core mathematical engines (pure Python/Pandas/SciPy).

> [!IMPORTANT]  
> All cross-module imports within the `src/` folder MUST use **Relative Imports** (e.g., `from .analyzers import ...` instead of `from analyzers import ...`). The root entry point scripts import via the `src` package, for instance: `from src.parser import ...`.

---

## 2. 🔀 Column Renaming Contract

Telemetry logs exported from the C# application contain system-specific prefixes. To ensure mathematical analyzers and visualizers (Plotly) work seamlessly, `TelemetryParser.validate_and_load()` must automatically rename raw telemetry columns to normalized Python variables:

| Raw C# Column (CSV) | Normalized Python Variable | Diagnostic Role |
|---|---|---|
| `Process.Threads` | `Threads` | Total thread count |
| `Process.ThreadsRunning` | `ThreadsRunning` | Active/running threads |
| `Memory.WorkingSetMB` | `WorkingSetMB` | Physical RAM occupied |
| `Process.ManagedHeapMB` | `ManagedHeapMB` | Application Managed Heap |

---

## 🧐 3. Mathematical Edge Cases & Anomaly Rules

### A. Connection Spike Detection (Rolling Z-Score)
* **Mandatory Shifted Window:** When calculating rolling mean ($\mu$) and rolling standard deviation ($\sigma$), **the series MUST be shifted by 1 cycle (`shift(1)`)** prior to calculations. If the current value is included, a massive spike will immediately inflate the baseline standard deviation ($\sigma_w$) and suppress the Z-Score, making it **impossible to detect the spike**.
* **Zero Standard Deviation ($\sigma = 0$) Edge Case:** When a system transitions from a perfectly constant state (std dev is 0) to a sudden spike:
  * If the new value differs from the mean: assign a fixed $Z = 99.0$ (Critical Anomaly) instead of returning `NaN` or `0.0`.
  * If the new value equals the mean: assign $Z = 0.0$.

### B. Resource Leak Detection (OLS Regression)
* **Statistical Significance check (`p-value`):** Trend slope checks ($\beta_1 > 0.05$) MUST be guarded by a statistical significance check **`p-value < 0.05`** using `scipy.stats.linregress` to prevent false positives when telemetry data length is short or carries high random noise.

---

## 🧩 4. Parsing Object Pools & Endpoints

* **Cleaning Generic C# Arity:** When parsing the `Pools` JSON array in `object_pools_metrics.csv`, generic generic arity arities (like `PacketContext`1`, `ObjectMap`2[[...]]`) must be simplified by stripping arity arity arities:
  ```python
  if '`' in type_str:
      type_str = type_str.split('`')[0]
  ```
* **Dynamic Pending Queue Source:** The `PendingByConnection` queue column resides in `dispatch_metrics.csv`, not `connections_metrics.csv`. Keep source directory scans dynamic.
