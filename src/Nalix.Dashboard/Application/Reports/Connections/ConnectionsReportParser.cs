using System.Text.Json;
using Nalix.Dashboard.Application.Abstractions;
using Nalix.Dashboard.Domain.Reports.Connections;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Reports.Connections;

internal sealed class ConnectionsReportParser : IReportParser<ConnectionsReport>
{
    public RuntimeObservationTarget Target => RuntimeObservationTarget.CONNECTIONS;

    public bool CanParse(RuntimeObservationTarget target) => target == RuntimeObservationTarget.CONNECTIONS;

    public object? Parse(ReadOnlyMemory<byte> ObservationData) => this.ParseTyped(ObservationData);

    public ConnectionsReport? ParseTyped(ReadOnlyMemory<byte> ObservationData)
    {
        if (ObservationData.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ConnectionsReport>(ObservationData.Span, ReportJsonOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
