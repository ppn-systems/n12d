// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Diagnostics;
using Nalix.Dashboard.Infrastructure.Statistics;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.Infrastructure.Statistics;

/// <summary>
/// Tests for <see cref="TrendDetector"/>.
/// </summary>
public sealed class TrendDetectorTests
{
    [Test]
    public void Detect_SteadyRise_ReturnsRisingTrendFinding()
    {
        const int n = 60;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + 5.0 * i + SeriesFixtures.Wobble(i);
        }

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(FindingKind.Trend, findings[0].Kind);
        Check.Near(300.0, findings[0].Score, 5.0);
        Check.Equal(times[n - 1], findings[0].At);
        Check.True(findings[0].Message.StartsWith("Rising +", StringComparison.Ordinal), $"unexpected message: {findings[0].Message}");
    }

    [Test]
    public void Detect_FlatNoisySeries_ReturnsNoFindings()
    {
        const int n = 60;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + ((i * 37) % 11 - 5) * 3.0;
        }

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(0, findings.Count);
    }

    [Test]
    public void Detect_NaNPairs_IgnoresThemAndReturnsTrend()
    {
        const int n = 60;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + 5.0 * i + SeriesFixtures.Wobble(i);
        }

        values[10] = double.NaN;
        values[30] = double.NaN;

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(times[n - 1], findings[0].At);
    }

    [Test]
    public void Detect_TooFewSamples_ReturnsNoFindings()
    {
        const int n = 10;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + 50.0 * i;
        }

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(0, findings.Count);
    }
}
