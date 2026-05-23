using Nalix.Dashboard.Application.Abstractions;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Reports;

internal sealed class ReportParserRegistry
{
    private readonly IReadOnlyDictionary<RuntimeObservationTarget, IReportParser> _parsers;

    public ReportParserRegistry(IEnumerable<IReportParser> parsers)
    {
        Dictionary<RuntimeObservationTarget, IReportParser> map = [];
        foreach (IReportParser parser in parsers)
        {
            map[parser.Target] = parser;
        }

        _parsers = map;
    }

    public IReportParser? Get(RuntimeObservationTarget target)
        => _parsers.TryGetValue(target, out IReportParser? parser) ? parser : null;

    public TReport? Parse<TReport>(RuntimeObservationTarget target, ReadOnlyMemory<byte> ObservationData) where TReport : class
    {
        if (!_parsers.TryGetValue(target, out IReportParser? parser) || parser is not IReportParser<TReport> typed)
        {
            return null;
        }

        return typed.ParseTyped(ObservationData);
    }
}
