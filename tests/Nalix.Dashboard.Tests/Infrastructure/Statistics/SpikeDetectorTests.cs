// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Diagnostics;
using Nalix.Dashboard.Infrastructure.Statistics;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.Infrastructure.Statistics;

/// <summary>
/// Tests for <see cref="SpikeDetector"/>.
/// </summary>
public sealed class SpikeDetectorTests
{
    [Test]
    public void Detect_InjectedSpikeInFlatSeries_ReturnsCriticalFinding()
    {
        const int n = 60;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = SeriesFixtures.MakeWobble(n);
        values[45] = 1000.0;

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(FindingKind.Spike, findings[0].Kind);
        Check.Equal(FindingSeverity.Critical, findings[0].Severity);
        Check.Equal("Test.Metric", findings[0].Metric);
        Check.Equal(times[45], findings[0].At);
        Check.True(findings[0].Score >= 6.0, $"score {findings[0].Score} should be at least 2 x threshold");
        Check.True(findings[0].Message.StartsWith("Spike: ", StringComparison.Ordinal), "message should start with 'Spike: '");
    }

    [Test]
    public void Detect_SmoothSeries_ReturnsNoFindings()
    {
        const int n = 120;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 100.0 + 10.0 * Math.Sin(i / 20.0);
        }

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(0, findings.Count);
    }

    [Test]
    public void Detect_ThreeSampleRun_ReturnsOneFinding()
    {
        const int n = 60;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = SeriesFixtures.MakeWobble(n);
        values[40] = 1000.0;
        values[41] = 1100.0;
        values[42] = 1050.0;

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        // One finding for the run. The first spike sample is the strongest: the windows of samples 41 and 42
        // already contain the earlier spike, which inflates their baseline spread.
        Check.Equal(1, findings.Count);
        Check.Equal(times[40], findings[0].At);
        Check.Equal(FindingSeverity.Critical, findings[0].Severity);
    }

    [Test]
    public void Detect_SeparatedSpikes_ReturnsOneFindingEach()
    {
        const int n = 80;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = SeriesFixtures.MakeWobble(n);
        values[30] = 1000.0;
        values[60] = 1000.0;

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(2, findings.Count);
        Check.Equal(times[30], findings[0].At);
        Check.Equal(times[60], findings[1].At);
    }

    [Test]
    public void Detect_ManySpikes_ReturnsTwentyMostRecent()
    {
        // 25 spikes spaced 40 samples apart: each baseline is clean, so all 25 are candidates.
        const int n = 1000;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = SeriesFixtures.MakeWobble(n);
        for (int k = 0; k < 25; k++)
        {
            values[20 + 40 * k] = 1000.0;
        }

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(20, findings.Count);
        Check.Equal(times[20 + 40 * 5], findings[0].At);
        Check.Equal(times[20 + 40 * 24], findings[19].At);
    }

    [Test]
    public void Detect_NaNSample_SkipsNaNAndReturnsSpike()
    {
        const int n = 60;
        long[] times = SeriesFixtures.MakeTimes(n);
        double[] values = SeriesFixtures.MakeWobble(n);
        values[45] = double.NaN;
        values[46] = 1000.0;

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(times[46], findings[0].At);
    }
}
