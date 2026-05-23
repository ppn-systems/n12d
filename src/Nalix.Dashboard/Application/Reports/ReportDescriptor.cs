using Nalix.Abstractions.Security;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Reports;

public sealed record ReportDescriptor(
    RuntimeObservationTarget Target,
    string Route,
    string Title,
    string Description,
    int DefaultPollingIntervalMs,
    int MinimumPollingIntervalMs,
    int MaximumPollingIntervalMs,
    bool SupportsRawJsonPreview,
    bool SupportsCharts,
    PermissionLevel RequiredPermissionLevel = PermissionLevel.SUPERVISOR);
