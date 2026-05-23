# Nalix.Dashboard

Blazor WebAssembly dashboard for Nalix observability reports.

## Run

```powershell
dotnet run --project .\src\Nalix.Dashboard\Nalix.Dashboard.csproj
```

Default launch URL: `http://localhost:5200`.

## Configure

Development defaults live in `appsettings.Development.json`:

```json
{
  "AdminClient": {
    "BackendHost": "127.0.0.1",
    "BackendPort": 57206,
    "ServerPublicKey": "<server-public-key>"
  }
}
```

Runtime settings such as WebSocket path, TLS, polling interval, and API key persistence are saved in browser storage from the Settings page.

## Example Use

1. Start a Nalix backend with `Nalix.Observability.Handlers`.
2. Run the dashboard.
3. Enter the `observability.private` key value on the login screen.
4. Use the metric pages to request runtime reports.
