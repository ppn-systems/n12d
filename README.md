# 📊 Nalix Observability Suite

[![Nalix Observability](docs/media/overview.png)](docs/media/overview.png)

A high-performance, real-time telemetry observation and post-mortem diagnostics ecosystem designed for **Nalix** applications. It tracks resource usage, message dispatch queues, active object pools, background tasks, and provides statistical models to pinpoint performance bottlenecks and resource leaks.

---

## 🗺️ Documentation Index

To help you get the most out of Nalix Observability, we have structured our documentation into three core guides:
* **📐 [Architecture & System Design](docs/ARCHITECTURE.md):** Learn how the telemetry events are defined, collected, transmitted via WebSockets, and analyzed.
* **🖥️ [Dashboard User Guide](docs/DASHBOARD_GUIDE.md):** A visual tour of the Blazor WebAssembly UI, detailing how to interpret metrics (Dispatch, Buffers, Object Pools, Tasks, etc.).
* **📈 [Telemetry Diagnostics Analyzer (Python)](analyzer/README.md):** Offline diagnostics engine utilizing rolling Z-scores and linear regressions to automate regression checks and pinpoint leaks.

---

## 📦 Project Ecosystem

The suite consists of the following components:

| Component | Path / Location | Tech Stack | Role |
| :--- | :--- | :--- | :--- |
| **Contracts** | `src/Nalix.Observability.Contracts/` | C# (.NET) | Shared packet payloads and telemetry events. |
| **Handlers** | `src/Nalix.Observability.Handlers/` | C# (.NET) | Low-overhead instrumentation engines capturing runtime data. |
| **Dashboard** | `src/Nalix.Dashboard/` | Blazor WebAssembly | Interactive Web UI rendering live metrics via WebSocket. |
| **Analyzer** | `analyzer/` | Python 3.10+ | Mathematical diagnostics script (CLI & Streamlit WebApp). |

---

## 🚀 Getting Started

### Prerequisites

This repository relies on Nalix core package dependencies from NuGet, version `12.5.0` (including `Nalix.SDK`, `Nalix.Codec`, `Nalix.Runtime`, and `Nalix.Framework`).

### 1. Build C# Solution

Restore dependencies and build the core packages:

```powershell
dotnet restore src\Nalix.Observability.sln
dotnet build src\Nalix.Observability.sln
```

### 2. Instrumentation: Backend Setup

Expose telemetry endpoints from your Nalix server by registering the observability handlers and binding a WebSocket server:

```csharp
using Nalix.Hosting;
using Nalix.Observability.Handlers;

NetworkApplication host = NetworkApplication.CreateBuilder()
    .AddHandler<ObservabilityAccessHandlers>()
    .AddHandler<RuntimeObservationHandlers>()
    .BindWebSocket<DefaultProtocol>()
        .OnPort(57207)
        .WithPath("/ws/")
        .Bind()
    .Build();
```

### 3. Launch the Live Dashboard

Run the Blazor WebAssembly Dashboard locally:

```powershell
dotnet run --project .\src\Nalix.Dashboard\Nalix.Dashboard.csproj
```

1. Open the URL shown in your terminal (typically `http://localhost:5000` or similar).
2. Enter your **64-character observability key** configured on the backend.
3. Direct the dashboard to the backend host (e.g., `ws://localhost:57207/ws/`) to start streaming real-time statistics.

---

## 📊 Offline Analytics & Troubleshooting

When diagnosing production issues retrospectively, you can import telemetry CSV files exported from the dashboard into the offline analyzer:

```powershell
# Run using standalone executable (No Python environment needed)
.\analyzer\dist\Nalix.Analyzer.exe --dir path\to\exported_csv_folder --format all
```

*For more information on setting up and running the Python-based diagnostics engine, refer to the [Nalix Telemetry Analyzer README](analyzer/README.md).*
