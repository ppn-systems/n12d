// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Controls;
using Nalix.Dashboard.State.Metrics;

namespace Nalix.Dashboard.Pages.Shared;

/// <summary>
/// The time window a page charts, and helpers that cut metric series down to it.
/// </summary>
public static class ChartWindow
{
    /// <summary>The selectable windows, value in seconds (0 = everything kept).</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> Options { get; } =
    [
        new("60", "1 min"),
        new("300", "5 min"),
        new("900", "15 min"),
        new("0", "All"),
    ];

    /// <summary>
    /// Builds a chart series holding only the samples newer than <paramref name="windowSeconds"/>.
    /// </summary>
    /// <param name="series">The source series, or <see langword="null"/>.</param>
    /// <param name="name">The legend name.</param>
    /// <param name="slot">The chart palette slot (1–7).</param>
    /// <param name="windowSeconds">The window; 0 keeps every sample.</param>
    /// <returns>The chart series, or <see langword="null"/> when there is no data.</returns>
    public static UiChartSeries? Slice(MetricSeries? series, string name, int slot, int windowSeconds)
    {
        if (series is null || series.Count == 0)
        {
            return null;
        }

        long cutoff = windowSeconds <= 0 ? long.MinValue : series.LatestTime - (windowSeconds * 1000L);
        int start = 0;
        while (start < series.Count && series.Times[start] < cutoff)
        {
            start++;
        }

        int count = series.Count - start;
        long[] times = new long[count];
        double[] values = new double[count];
        for (int i = 0; i < count; i++)
        {
            times[i] = series.Times[start + i];
            values[i] = series.Values[start + i];
        }

        return new UiChartSeries(name, times, values, UiTone.Accent, slot);
    }
}
