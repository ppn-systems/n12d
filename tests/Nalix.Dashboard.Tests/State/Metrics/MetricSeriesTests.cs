// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.State.Metrics;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.State.Metrics;

/// <summary>
/// Tests for <see cref="MetricSeries"/>.
/// </summary>
public sealed class MetricSeriesTests
{
    [Test]
    public void Add_OverCapacity_KeepsNewestInOrder()
    {
        MetricSeries series = new("x", 3);
        for (int i = 1; i <= 5; i++)
        {
            series.Add(i, i * 10);
        }

        Check.Equal(3, series.Count);
        Check.Near(30, series.Values[0], 0);
        Check.Near(50, series.Values[2], 0);
        Check.Equal(5L, series.Times[2]);
        Check.Near(50, series.Latest, 0);
    }
}
