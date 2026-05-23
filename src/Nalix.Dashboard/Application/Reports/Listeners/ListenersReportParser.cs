using System.Text.Json;
using Nalix.Dashboard.Application.Abstractions;
using Nalix.Dashboard.Domain.Reports.Listeners;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Reports.Listeners;

internal sealed class ListenersReportParser : IReportParser<ListenersReport>
{
    public RuntimeObservationTarget Target => RuntimeObservationTarget.LISTENER;

    public bool CanParse(RuntimeObservationTarget target) => target == RuntimeObservationTarget.LISTENER;

    public object? Parse(ReadOnlyMemory<byte> ObservationData) => this.ParseTyped(ObservationData);

    public ListenersReport? ParseTyped(ReadOnlyMemory<byte> ObservationData)
    {
        if (ObservationData.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ListenersReport>(ObservationData.Span, ReportJsonOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
