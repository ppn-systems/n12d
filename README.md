# Nalix Observability

[![GitHub repository](https://img.shields.io/badge/GitHub-ppn--systems%2Fn12d-blue?logo=github)](https://github.com/ppn-systems/n12d)
[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![LICENSE](https://img.shields.io/github/license/ppn-systems/nalix?style=flat-square)](License)

[![Nalix Observability](docs/media/overview.png)](docs/media/overview.png)

Small observability bundle for Nalix apps:

- `Nalix.Observability.Contracts`: packets shared by backend and clients.
- `Nalix.Observability.Handlers`: backend handlers that expose access and runtime reports.
- `Nalix.Dashboard`: Blazor WebAssembly dashboard for reading reports over WebSocket.

---

## 🗺️ Documentation Index

To help you get the most out of Nalix Observability, we have structured our documentation into three core guides:

* **📐 [Architecture & System Design](docs/ARCHITECTURE.md):** Learn how the telemetry events are defined, collected, transmitted via WebSockets, and analyzed.
* **🖥️ [Dashboard User Guide](docs/DASHBOARD_GUIDE.md):** A visual tour of the Blazor WebAssembly UI, detailing how to interpret metrics (Dispatch, Buffers, Object Pools, Tasks, etc.).
* **📈 [Telemetry Diagnostics Analyzer (Python)](analyzer/README.md):** Offline diagnostics engine utilizing rolling Z-scores and linear regressions to automate regression checks and pinpoint leaks.
* **📜 [Analyzer Developer Guidelines](analyzer/DEVELOPMENT.md):** Development rules covering column contracts, rolling Z-score math safeguards, and parsing arity cleanups.
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

```powershell
dotnet restore src\Nalix.Observability.sln
dotnet build src\Nalix.Observability.sln
```

## Example Use

Backend setup:

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

Dashboard:

```powershell
dotnet run --project .\src\Nalix.Dashboard\Nalix.Dashboard.csproj
```

Open the shown URL, enter the 64-character observability key, then point the dashboard to the backend host, port, and WebSocket path.

---

## 🛠️ Developer Resources

If you are extending or modifying the Nalix Diagnostics Engine:

* Please review the **[Development Rules & Guidelines](analyzer/DEVELOPMENT.md)** to understand our strict column renaming contracts, rolling Z-score shifted window rules, $p$-value statistical thresholds, and type pool name formatting conventions.
