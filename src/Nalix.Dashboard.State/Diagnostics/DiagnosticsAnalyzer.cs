// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using Nalix.Dashboard.Abstractions.Diagnostics;
using Nalix.Dashboard.Infrastructure.Statistics;
using Nalix.Dashboard.State.Metrics;

namespace Nalix.Dashboard.State.Diagnostics;

/// <summary>
/// Runs the spike and trend detectors over the metrics where those anomalies mean something.
/// </summary>
/// <remarks>
/// Only curated metrics are analysed: running both detectors over every leaf of every report would cost
/// far more than it finds, and a "trend" in a cumulative counter is expected, not a leak.
/// </remarks>
public sealed class DiagnosticsAnalyzer
{
    // Sudden deviation matters: latency, CPU, queues, contention, connection churn.
    private static readonly string[] s_spikeMetrics =
    [
        MetricPaths.CpuPercent,
        MetricPaths.RoundTripMs,
        MetricPaths.AllocationBytesPerSecond,
        MetricPaths.GcPerSecond,
        "Process.ThreadPoolPendingItems",
        "Process.GcPauseTimePercent",
        "Connections.TotalConnections",
        "Connections.IngressBytesPerSecond",
        "Connections.EgressBytesPerSecond",
        "Connections.TotalPacketsDropped",
        "Dispatch.TotalPackets",
        "Dispatch.PipelineMetrics.ActiveExecutions",
        "Dispatch.PipelineMetrics.MaxTimeMs",
        "Dispatch.PipelineMetrics.TotalErrors",
    ];

    // Sustained growth matters: memory, threads, handles, timers, registrations.
    private static readonly string[] s_trendMetrics =
    [
        "Process.WorkingSetBytes",
        "Process.ManagedHeapBytes",
        "Process.HeapSizeBytes",
        "Process.CommittedBytes",
        "Process.FragmentedBytes",
        "Process.ThreadPoolThreads",
        "Process.ActiveTimers",
        "Tasks.Process.Threads",
        "Tasks.Process.Handles",
        "Connections.TotalConnections",
        "Connections.TimingWheel.RegisteredCount",
    ];

    private readonly SpikeDetector _spikes = new();
    private readonly TrendDetector _trends = new();

    /// <summary>
    /// Analyses <paramref name="history"/>.
    /// </summary>
    /// <param name="history">The metric history.</param>
    /// <returns>Findings ordered by severity, then most recent first.</returns>
    public List<DiagnosticFinding> Analyze(MetricHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        List<DiagnosticFinding> findings = [];

        RUN(history, s_spikeMetrics, findings, spikes: true);
        RUN(history, s_trendMetrics, findings, spikes: false);

        findings.Sort(static (a, b) =>
        {
            int bySeverity = b.Severity.CompareTo(a.Severity);
            return bySeverity != 0 ? bySeverity : b.At.CompareTo(a.At);
        });

        return findings;
    }

    private void RUN(MetricHistory history, string[] metrics, List<DiagnosticFinding> findings, bool spikes)
    {
        foreach (string metric in metrics)
        {
            MetricSeries? series = history.Get(metric);
            if (series is null || series.Count < 3)
            {
                continue;
            }

            long[] times = ArrayPool<long>.Shared.Rent(series.Count);
            double[] values = ArrayPool<double>.Shared.Rent(series.Count);
            try
            {
                series.CopyTo(times, values);
                ReadOnlySpan<long> t = times.AsSpan(0, series.Count);
                ReadOnlySpan<double> v = values.AsSpan(0, series.Count);

                if (spikes)
                {
                    _spikes.Detect(metric, t, v, findings);
                }
                else
                {
                    _trends.Detect(metric, t, v, findings);
                }
            }
            finally
            {
                ArrayPool<long>.Shared.Return(times);
                ArrayPool<double>.Shared.Return(values);
            }
        }
    }
}
