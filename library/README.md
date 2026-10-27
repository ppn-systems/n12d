# library

Binary build of `Nalix.Observability.Contracts` from
[`ppn-systems/nalix/extensions/Nalix.Observability.Contracts`](https://github.com/ppn-systems/nalix/tree/master/extensions/Nalix.Observability.Contracts).
The contracts are not published to NuGet, so the dashboard references this DLL directly.

Refresh after a contract change:

```bash
dotnet build <nalix>/extensions/Nalix.Observability.Contracts -c Release
cp <nalix>/extensions/Nalix.Observability.Contracts/bin/Release/net10.0/Nalix.Observability.Contracts.{dll,xml} library/
```
