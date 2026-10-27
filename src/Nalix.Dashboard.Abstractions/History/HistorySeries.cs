// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.History;

/// <summary>
/// One metric's bucketed history; buckets without samples are NaN.
/// </summary>
/// <param name="Name">The metric path.</param>
/// <param name="Average">Bucket averages.</param>
/// <param name="Maximum">Bucket maxima.</param>
public sealed record HistoryMetric(string Name, double[] Average, double[] Maximum);

/// <summary>
/// Recorded history of several metrics over a range, folded server-side into equal buckets.
/// </summary>
/// <param name="From">Range start, Unix milliseconds.</param>
/// <param name="To">Range end, Unix milliseconds.</param>
/// <param name="StepMs">Bucket width.</param>
/// <param name="Times">Bucket start times.</param>
/// <param name="Metrics">The requested metrics in request order.</param>
public sealed record HistorySeries(long From, long To, long StepMs, long[] Times, IReadOnlyList<HistoryMetric> Metrics);
