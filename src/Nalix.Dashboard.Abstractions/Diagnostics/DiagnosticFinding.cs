// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Diagnostics;

/// <summary>
/// The kind of anomaly a detector reports.
/// </summary>
public enum FindingKind : byte
{
    /// <summary>A sudden deviation from the recent rolling baseline (rolling z-score).</summary>
    Spike,

    /// <summary>A statistically significant upward trend, such as a memory or handle leak (least-squares regression).</summary>
    Trend
}

/// <summary>
/// How urgent a finding is.
/// </summary>
public enum FindingSeverity : byte
{
    /// <summary>Worth a look.</summary>
    Info,

    /// <summary>Likely a problem.</summary>
    Warning,

    /// <summary>Almost certainly a problem.</summary>
    Critical
}

/// <summary>
/// One anomaly detected in a metric series.
/// </summary>
/// <param name="Kind">The anomaly kind.</param>
/// <param name="Severity">The urgency.</param>
/// <param name="Metric">The metric path, e.g. <c>Process.WorkingSetBytes</c>.</param>
/// <param name="Message">A human-readable explanation.</param>
/// <param name="Score">The detector score: |z| for spikes, growth per minute for trends.</param>
/// <param name="At">The Unix-millisecond time of the anomalous sample (latest sample for trends).</param>
public sealed record DiagnosticFinding(FindingKind Kind, FindingSeverity Severity, string Metric, string Message, double Score, long At);
