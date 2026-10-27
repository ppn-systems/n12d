# n12d — Nalix Dashboard

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/ppn-systems/n12d?style=flat-square)](LICENSE)

A live monitoring dashboard for [Nalix](https://github.com/ppn-systems/nalix) servers, built with Blazor WebAssembly.
It talks to the server over the same encrypted Nalix WebSocket channel the application already uses — no
extra HTTP endpoint, no agent, no metrics database.

- **One request per tick.** The server samples every subsystem the open views need into a single JSON payload
  (compressed by the Nalix frame pipeline), instead of one round-trip per report.
- **Every report, no hand-written parsers.** Reports render generically from their JSON, so new server-side
  fields show up without a dashboard release. Click any number to chart its history.
- **Diagnostics in the browser.** Rolling z-score spike detection and least-squares leak detection (with a
  Student-t p-value) run over the kept history.
- **Recordings.** Capture every metric of every sample and export a wide CSV.
- **No third-party packages.** Only the Blazor WebAssembly runtime and the first-party `Nalix.SDK`. Controls,
  SVG charts, styling and statistics are written in this repository.

## Server setup

Add the [`Nalix.Observability`](https://github.com/ppn-systems/nalix/tree/master/extensions/Nalix.Observability)
extension to the server and expose a WebSocket listener:

```csharp
NetworkApplication host = NetworkApplication.CreateBuilder()
    .UseObservability()
    .ListenWebSocket<DefaultProtocol>()
        .OnPort(57207)
        .WithPath("/ws/")
        .Bind()
    .Build();
```

On first start the server writes a random 64-character hex access key to `observability.private` in its
configuration directory. That key grants supervisor access; treat it like a password.

## Run the dashboard

```bash
dotnet run --project src/Nalix.Dashboard
```

Open the printed URL, enter host, port, path and the access key. The key is kept in session storage (forgotten
when the browser closes) unless you tick *Remember the key on this device*.

The published build is a static site (`dotnet publish src/Nalix.Dashboard -c Release`); the
[deploy workflow](.github/workflows/deploy.yml) deploys it to Cloudflare Pages.

## Build and test

```bash
dotnet build src/Nalix.Dashboard.slnx -c Release
dotnet run --project tests/Nalix.Dashboard.Tests -c Release
```

The test runner is dependency-free; exit code 0 means every test passed.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the project layout and data flow.
