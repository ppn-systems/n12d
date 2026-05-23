# Nalix Observability

Small observability bundle for Nalix apps:

- `Nalix.Observability.Contracts`: packets shared by backend and clients.
- `Nalix.Observability.Handlers`: backend handlers that expose access and runtime reports.
- `Nalix.Dashboard`: Blazor WebAssembly dashboard for reading reports over WebSocket.

## Build

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
