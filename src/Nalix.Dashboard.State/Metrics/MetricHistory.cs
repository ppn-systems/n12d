// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Reports;

namespace Nalix.Dashboard.State.Metrics;

/// <summary>
/// Keeps a <see cref="MetricSeries"/> for every numeric leaf seen in observation snapshots.
/// </summary>
/// <remarks>
/// Paths join property names with dots. Array items are addressed by their <c>Name</c>, <c>Type</c>,
/// <c>Id</c> or <c>Key</c> string when they have one (stable across samples), otherwise by index:
/// <c>Dispatch.MiddlewareMetrics[AuthMiddleware].TotalExecutions</c>.
/// </remarks>
public sealed class MetricHistory
{
    private const int MaxSeries = 4096;
    private static readonly string[] s_itemKeys = ["Name", "Type", "Id", "Key"];

    private readonly Dictionary<string, MetricSeries> _series = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Parent, string Child), string> _paths = [];
    private double _lastCpuMs = double.NaN;
    private double _lastAllocated = double.NaN;
    private double _lastGcCount = double.NaN;
    private long _lastSampledAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricHistory"/> class.
    /// </summary>
    /// <param name="capacity">Samples kept per series.</param>
    public MetricHistory(int capacity) => Capacity = capacity;

    /// <summary>Gets the samples kept per series.</summary>
    public int Capacity { get; }

    /// <summary>Gets the number of tracked series.</summary>
    public int Count => _series.Count;

    /// <summary>Gets all series keyed by path.</summary>
    public IReadOnlyDictionary<string, MetricSeries> Series => _series;

    /// <summary>
    /// Gets a series by path.
    /// </summary>
    /// <param name="path">The metric path.</param>
    /// <returns>The series, or <see langword="null"/> when never seen.</returns>
    public MetricSeries? Get(string path) => _series.GetValueOrDefault(path);

    /// <summary>
    /// Records every numeric leaf of <paramref name="snapshot"/> plus the derived metrics.
    /// </summary>
    /// <param name="snapshot">The snapshot.</param>
    public void Record(ObservationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        long t = snapshot.SampledAt;

        foreach (ReportNode section in snapshot.Root.Children)
        {
            WALK(section, section.Name, t);
        }

        ADD(MetricPaths.RoundTripMs, t, snapshot.RoundTripMs);
        ADD(MetricPaths.PayloadBytes, t, snapshot.PayloadBytes);
        DERIVE(snapshot, t);
    }

    private void WALK(ReportNode node, string path, long t)
    {
        switch (node.Kind)
        {
            case ReportNodeKind.Number:
                ADD(path, t, node.Number);
                break;

            case ReportNodeKind.Object:
                foreach (ReportNode child in node.Children)
                {
                    WALK(child, JOIN(path, child.Name, '.'), t);
                }

                break;

            case ReportNodeKind.Array:
                foreach (ReportNode item in node.Children)
                {
                    WALK(item, JOIN(path, ITEM_KEY(item), '['), t);
                }

                break;

            default:
                break;
        }
    }

    private void DERIVE(ObservationSnapshot snapshot, long t)
    {
        ReportNode? process = snapshot.Get(Sections.Process);
        if (process is null)
        {
            return;
        }

        double elapsedMs = t - _lastSampledAt;
        bool hasPrevious = _lastSampledAt > 0 && elapsedMs > 0;

        if (process.TryGetNumber("CpuTimeMs", out double cpuMs) && process.TryGetNumber("ProcessorCount", out double cores) && cores > 0)
        {
            if (hasPrevious && !double.IsNaN(_lastCpuMs) && cpuMs >= _lastCpuMs)
            {
                ADD(MetricPaths.CpuPercent, t, Math.Clamp((cpuMs - _lastCpuMs) / (elapsedMs * cores) * 100, 0, 100));
            }

            _lastCpuMs = cpuMs;
        }

        if (process.TryGetNumber("TotalAllocatedBytes", out double allocated))
        {
            if (hasPrevious && !double.IsNaN(_lastAllocated) && allocated >= _lastAllocated)
            {
                ADD(MetricPaths.AllocationBytesPerSecond, t, (allocated - _lastAllocated) * 1000 / elapsedMs);
            }

            _lastAllocated = allocated;
        }

        if (process.TryGetNumber("Gen0Collections", out double gen0) && process.TryGetNumber("Gen1Collections", out double gen1) && process.TryGetNumber("Gen2Collections", out double gen2))
        {
            double gcCount = gen0 + gen1 + gen2;
            if (hasPrevious && !double.IsNaN(_lastGcCount) && gcCount >= _lastGcCount)
            {
                ADD(MetricPaths.GcPerSecond, t, (gcCount - _lastGcCount) * 1000 / elapsedMs);
            }

            _lastGcCount = gcCount;
        }

        _lastSampledAt = t;
    }

    private void ADD(string path, long t, double value)
    {
        if (!_series.TryGetValue(path, out MetricSeries? series))
        {
            if (_series.Count >= MaxSeries)
            {
                return;
            }

            series = new MetricSeries(path, Capacity);
            _series.Add(path, series);
        }

        series.Add(t, value);
    }

    private string JOIN(string parent, string child, char separator)
    {
        if (_paths.TryGetValue((parent, child), out string? path))
        {
            return path;
        }

        path = separator == '[' ? $"{parent}[{child}]" : $"{parent}.{child}";
        if (_paths.Count < MaxSeries * 4)
        {
            _paths[(parent, child)] = path;
        }

        return path;
    }

    private static string ITEM_KEY(ReportNode item)
    {
        if (item.Kind == ReportNodeKind.Object)
        {
            foreach (string key in s_itemKeys)
            {
                ReportNode? candidate = item.Get(key);
                if (candidate is { Kind: ReportNodeKind.String, Text.Length: > 0 })
                {
                    return candidate.Text;
                }
            }
        }

        return item.Name;
    }
}
