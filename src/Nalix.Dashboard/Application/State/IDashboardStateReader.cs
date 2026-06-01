using Nalix.Dashboard.Domain.Logs;
using Nalix.Dashboard.Domain.Metrics;
using Nalix.Dashboard.Domain.Reports;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.State;

internal interface IDashboardStateReader
{
    event Action? Changed;
    event Action<DashboardReportSnapshot>? ReportUpdated;

    bool IsConnected { get; }

    bool HasApiKey { get; }

    string? LastError { get; }

    double? LastPingMilliseconds { get; }

    DateTimeOffset? LastPingAt { get; }

    IReadOnlyDictionary<RuntimeObservationTarget, DashboardReportSnapshot> Reports { get; }

    IReadOnlyList<DashboardPingSample> PingSamples { get; }

    IReadOnlyList<DashboardLogEntry> Logs { get; }
}
