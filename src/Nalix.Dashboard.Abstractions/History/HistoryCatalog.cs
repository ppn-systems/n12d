// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.History;

/// <summary>
/// One recorded day on the server.
/// </summary>
/// <param name="Date">The UTC date.</param>
/// <param name="From">First sample, Unix milliseconds.</param>
/// <param name="To">Last sample, Unix milliseconds.</param>
/// <param name="Samples">Number of samples.</param>
/// <param name="Bytes">Size of the day file.</param>
public sealed record HistoryDay(DateOnly Date, long From, long To, int Samples, long Bytes);

/// <summary>
/// What the server has recorded in a range.
/// </summary>
/// <param name="IntervalMs">The recording interval.</param>
/// <param name="Days">Recorded days, oldest first.</param>
/// <param name="Metrics">Every metric name recorded in those days, sorted.</param>
public sealed record HistoryCatalog(long IntervalMs, IReadOnlyList<HistoryDay> Days, IReadOnlyList<string> Metrics);
