# Architecture

## Layout

```text
n12d/
├── src/                                   # Nalix.Dashboard.slnx
│   ├── Nalix.Dashboard/                   # Blazor WebAssembly host: Program.cs, App.razor, wwwroot (index.html, css, 1 small js)
│   ├── Nalix.Dashboard.Pages/             # Razor pages and layout (Overview, Section, Metric, Diagnostics, Recordings, Log, Settings)
│   ├── Nalix.Dashboard.Controls/          # Design system: tokens.css, ui.css, Ui* components, SVG sparkline/line chart, UiFormat
│   ├── Nalix.Dashboard.State/             # DashboardStore (connection, sampling loop, reconnect), MetricHistory, MetricRecorder, DiagnosticsAnalyzer
│   ├── Nalix.Dashboard.Transport/         # NalixObservabilityClient (Nalix.SDK WebSocketSession), ReportJsonReader
│   ├── Nalix.Dashboard.Infrastructure/    # Statistics (z-score, OLS, Student-t), CSV export, browser storage and download
│   └── Nalix.Dashboard.Abstractions/      # ReportNode, SectionInfo catalog, ObservationSnapshot, client/settings interfaces
├── tests/Nalix.Dashboard.Tests/           # Dependency-free test runner (dotnet run)
├── library/                               # Vendored Nalix.Observability.Contracts.dll (not on NuGet)
└── Directory.Build.props · Directory.Packages.props · Version.props · global.json
```

Dependencies point downward only:

```text
Nalix.Dashboard (host)
 ├── Pages ──► Controls ──► Abstractions
 │     └────► State ──► Infrastructure ──► Abstractions
 └── Transport ──► Abstractions ──► Nalix.SDK, Nalix.Observability.Contracts
```

`State` knows the transport only through `IObservabilityClient`; the host wires `NalixObservabilityClient`.
Pages never touch the socket.

## Data flow

1. `DashboardStore` runs one `PeriodicTimer`. Each tick it builds a target mask: the overview basics
   (`PROCESS | CONNECTIONS | DISPATCH`), plus whatever the open views registered with `Watch(...)`, or `ALL`
   while recording.
2. `NalixObservabilityClient` sends one encrypted `RuntimeObservation` request. The server
   (`Nalix.Observability`) writes every requested section into one JSON object and replies; the Nalix frame
   pipeline compresses it when worthwhile.
3. `ReportJsonReader` parses the payload into an immutable `ReportNode` tree, decoding property names on the
   stack and reusing cached strings, since the same names arrive every tick.
4. `MetricHistory` appends every numeric leaf to a fixed-size ring buffer keyed by path
   (`Dispatch.MiddlewareMetrics[Auth].TotalExecutions`) and derives rates from cumulative counters
   (CPU %, allocation rate, GC/s). Charts bind to the ring buffers' allocation-free views.
5. Views re-render on `DashboardStore.Changed`.

On a dropped connection or three consecutive failed samples the store reconnects with exponential backoff
(1 s → 30 s) using the in-memory key.

## Protocol

| Opcode | Packet | Permission |
| :--- | :--- | :--- |
| `0x00FF` | `ObservabilityAccess` — presents the 32-byte key, grants `SUPERVISOR` | `NONE` |
| `0x00FE` | `RuntimeObservation` — `Targets` mask in, one JSON object keyed by section out | `SUPERVISOR` |

The contracts live in `nalix/extensions/Nalix.Observability.Contracts`; refresh `library/` after a change
(see `library/README.md`).

## Conventions

- C# preview on .NET 10, nullable enabled, warnings are errors, code style enforced in build (`.editorconfig`).
- No third-party packages: `Directory.Packages.props` lists the whole dependency surface.
- Hot paths (parsing, history) avoid LINQ and per-sample allocations where it is cheap to do so.
- Release publish strips symbols and XML docs from the downloadable output.
