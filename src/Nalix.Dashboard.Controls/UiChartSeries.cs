// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Controls;

/// <summary>
/// One line of a <see cref="UiLineChart"/>. <paramref name="Times"/> and <paramref name="Values"/>
/// are parallel lists of Unix milliseconds and values; non-finite values are skipped.
/// </summary>
/// <param name="Name">The legend label.</param>
/// <param name="Times">The sample times, in Unix milliseconds.</param>
/// <param name="Values">The sample values.</param>
/// <param name="Tone">The line colour, used when <paramref name="Slot"/> is null.</param>
/// <param name="Slot">An optional palette slot (1 to 7, clamped). When set, the line uses <c>--chart-{Slot}</c>.</param>
public sealed record UiChartSeries(string Name, IReadOnlyList<long> Times, IReadOnlyList<double> Values, UiTone Tone, int? Slot = null);
