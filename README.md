# Nalix Observability

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

## Packages

This repo uses Nalix packages from NuGet, version `12.5.0`, for core dependencies such as `Nalix.SDK`, `Nalix.Codec`, `Nalix.Runtime`, and `Nalix.Framework`.

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
