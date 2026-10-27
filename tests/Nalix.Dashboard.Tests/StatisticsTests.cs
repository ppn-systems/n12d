// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Diagnostics;
using Nalix.Dashboard.Infrastructure.Statistics;

namespace Nalix.Dashboard.Tests;

/// <summary>
/// Tests for <see cref="SeriesMath"/>, <see cref="SpikeDetector"/> and <see cref="TrendDetector"/>.
/// </summary>
internal static class StatisticsTests
{
    private const long BaseTimeMs = 1_700_000_000_000L;
    private const long OneSecondMs = 1000L;

    /// <summary>
    /// Registers every statistics test with the runner.
    /// </summary>
    /// <param name="tests">The runner's test list.</param>
    public static void Register(List<(string Name, Action Body)> tests)
    {
        tests.Add(("Mean_KnownData_ReturnsAverageAndNaNForEmpty", Mean_KnownData_ReturnsAverageAndNaNForEmpty));
        tests.Add(("StandardDeviation_KnownData_UsesSampleDenominator", StandardDeviation_KnownData_UsesSampleDenominator));
        tests.Add(("ZScore_KnownWindow_ReturnsStandardisedDistance", ZScore_KnownWindow_ReturnsStandardisedDistance));
        tests.Add(("ZScore_ZeroSpreadEqualValue_ReturnsZero", ZScore_ZeroSpreadEqualValue_ReturnsZero));
        tests.Add(("ZScore_ZeroSpreadDifferentValue_ReturnsSentinel", ZScore_ZeroSpreadDifferentValue_ReturnsSentinel));
        tests.Add(("ZScore_ConstantWindowWithRoundingNoise_TreatedAsZeroSpread", ZScore_ConstantWindowWithRoundingNoise_TreatedAsZeroSpread));
        tests.Add(("Fit_ExactLine_RecoversSlopeInterceptAndPerfectFit", Fit_ExactLine_RecoversSlopeInterceptAndPerfectFit));
        tests.Add(("Fit_SmallNoisySample_MatchesHandComputedLeastSquares", Fit_SmallNoisySample_MatchesHandComputedLeastSquares));
        tests.Add(("Fit_LongDeterministicNoise_RecoversKnownSlope", Fit_LongDeterministicNoise_RecoversKnownSlope));
        tests.Add(("Fit_ConstantX_ReturnsZeroSlopeAndPValueOne", Fit_ConstantX_ReturnsZeroSlopeAndPValueOne));
        tests.Add(("Fit_FewerThanThreePoints_ReturnsPValueOne", Fit_FewerThanThreePoints_ReturnsPValueOne));
        tests.Add(("StudentT_ReferenceValues_MatchTables", StudentT_ReferenceValues_MatchTables));
        tests.Add(("StudentT_ZeroStatistic_ReturnsOneAndIsSymmetric", StudentT_ZeroStatistic_ReturnsOneAndIsSymmetric));
        tests.Add(("IncompleteBeta_KnownIdentities_Hold", IncompleteBeta_KnownIdentities_Hold));
        tests.Add(("LogGamma_KnownFactorials_Hold", LogGamma_KnownFactorials_Hold));
        tests.Add(("Spike_InjectedSpikeInFlatSeries_IsFlaggedCritical", Spike_InjectedSpikeInFlatSeries_IsFlaggedCritical));
        tests.Add(("Spike_SmoothSeries_IsNotFlagged", Spike_SmoothSeries_IsNotFlagged));
        tests.Add(("Spike_ThreeSampleRun_CollapsesToOneFinding", Spike_ThreeSampleRun_CollapsesToOneFinding));
        tests.Add(("Spike_SeparatedSpikes_AreReportedSeparately", Spike_SeparatedSpikes_AreReportedSeparately));
        tests.Add(("Spike_ManySpikes_CapsAtTwentyKeepingMostRecent", Spike_ManySpikes_CapsAtTwentyKeepingMostRecent));
        tests.Add(("Spike_NaNSample_IsSkippedAndDoesNotBreakScoring", Spike_NaNSample_IsSkippedAndDoesNotBreakScoring));
        tests.Add(("Trend_SteadyRise_IsFlagged", Trend_SteadyRise_IsFlagged));
        tests.Add(("Trend_FlatNoisySeries_IsNotFlagged", Trend_FlatNoisySeries_IsNotFlagged));
        tests.Add(("Trend_NaNPairsAreIgnored", Trend_NaNPairsAreIgnored));
        tests.Add(("Trend_TooFewSamples_IsNotFlagged", Trend_TooFewSamples_IsNotFlagged));
    }

    private static void Mean_KnownData_ReturnsAverageAndNaNForEmpty()
    {
        Check.Near(2.5, SeriesMath.Mean([1.0, 2.0, 3.0, 4.0]), 1e-12);
        Check.True(double.IsNaN(SeriesMath.Mean(ReadOnlySpan<double>.Empty)), "mean of empty must be NaN");
    }

    private static void StandardDeviation_KnownData_UsesSampleDenominator()
    {
        // Mean 5, sum of squared deviations 32, sample variance 32 / 7.
        Check.Near(Math.Sqrt(32.0 / 7.0), SeriesMath.StandardDeviation([2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0]), 1e-12);
        Check.Near(0.0, SeriesMath.StandardDeviation([5.0]), 0.0);
    }

    private static void ZScore_KnownWindow_ReturnsStandardisedDistance()
    {
        // Window [1..5]: mean 3, sample standard deviation sqrt(2.5).
        double sd = Math.Sqrt(2.5);
        Check.Near(2.0, SeriesMath.ZScore([1.0, 2.0, 3.0, 4.0, 5.0], 3.0 + 2.0 * sd), 1e-9);
        Check.Near(-1.0, SeriesMath.ZScore([1.0, 2.0, 3.0, 4.0, 5.0], 3.0 - sd), 1e-9);
    }

    private static void ZScore_ZeroSpreadEqualValue_ReturnsZero()
    {
        Check.Near(0.0, SeriesMath.ZScore([5.0, 5.0, 5.0], 5.0), 0.0);
    }

    private static void ZScore_ZeroSpreadDifferentValue_ReturnsSentinel()
    {
        Check.Near(SeriesMath.InfiniteZScore, SeriesMath.ZScore([5.0, 5.0, 5.0], 6.0), 0.0);
        Check.Near(SeriesMath.InfiniteZScore, SeriesMath.ZScore([5.0, 5.0, 5.0], 4.0), 0.0);
        Check.Near(99.0, SeriesMath.InfiniteZScore, 0.0);
    }

    private static void ZScore_ConstantWindowWithRoundingNoise_TreatedAsZeroSpread()
    {
        // Summing 0.1 seven times and dividing does not return exactly 0.1; the zero-spread rule must still apply.
        double[] window = [0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1];
        Check.Near(0.0, SeriesMath.ZScore(window, 0.1), 0.0);
    }

    private static void Fit_ExactLine_RecoversSlopeInterceptAndPerfectFit()
    {
        double[] x = new double[10];
        double[] y = new double[10];
        for (int i = 0; i < x.Length; i++)
        {
            x[i] = i;
            y[i] = 2.0 * i + 1.0;
        }

        LinearFit fit = SeriesMath.Fit(x, y);
        Check.Near(2.0, fit.Slope, 1e-12);
        Check.Near(1.0, fit.Intercept, 1e-12);
        Check.Near(1.0, fit.RSquared, 1e-12);
        Check.Near(0.0, fit.PValue, 0.0);
        Check.Equal(10, fit.Count);
    }

    private static void Fit_SmallNoisySample_MatchesHandComputedLeastSquares()
    {
        // x = 0..4, y = 1,3,2,4,5: Sxx = 10, Sxy = 9, Syy = 10 -> slope 0.9, intercept 1.2, R^2 = 0.81.
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] y = [1.0, 3.0, 2.0, 4.0, 5.0];

        LinearFit fit = SeriesMath.Fit(x, y);
        Check.Near(0.9, fit.Slope, 1e-12);
        Check.Near(1.2, fit.Intercept, 1e-12);
        Check.Near(0.81, fit.RSquared, 1e-12);
        // t = 3.5762 with 3 degrees of freedom; reference p from numerical integration of the t density.
        Check.Near(0.037386, fit.PValue, 1e-5);
    }

    private static void Fit_LongDeterministicNoise_RecoversKnownSlope()
    {
        // True slope 0.5 with a zero-mean-ish periodic wobble of amplitude 0.5.
        const int n = 60;
        double[] x = new double[n];
        double[] y = new double[n];
        for (int i = 0; i < n; i++)
        {
            x[i] = i;
            y[i] = 0.5 * i + 10.0 + ((i * 37) % 11 - 5) * 0.1;
        }

        LinearFit fit = SeriesMath.Fit(x, y);
        Check.Near(0.5, fit.Slope, 0.01);
        Check.True(fit.PValue < 1e-6, $"slope should be highly significant, p = {fit.PValue}");
        Check.True(fit.RSquared > 0.99, $"R^2 should be near 1, was {fit.RSquared}");
    }

    private static void Fit_ConstantX_ReturnsZeroSlopeAndPValueOne()
    {
        LinearFit fit = SeriesMath.Fit([3.0, 3.0, 3.0, 3.0], [1.0, 2.0, 3.0, 4.0]);
        Check.Near(0.0, fit.Slope, 0.0);
        Check.Near(1.0, fit.PValue, 0.0);
    }

    private static void Fit_FewerThanThreePoints_ReturnsPValueOne()
    {
        LinearFit fit = SeriesMath.Fit([0.0, 1.0], [0.0, 1.0]);
        Check.Near(0.0, fit.Slope, 0.0);
        Check.Near(1.0, fit.PValue, 0.0);
        Check.Equal(2, fit.Count);
    }

    private static void StudentT_ReferenceValues_MatchTables()
    {
        Check.Near(0.0734, SeriesMath.StudentTTwoSidedPValue(2.0, 10), 0.0005);
        Check.Near(0.05, SeriesMath.StudentTTwoSidedPValue(12.706, 1), 0.001);
        Check.Near(0.0503, SeriesMath.StudentTTwoSidedPValue(1.96, 1000), 0.001);
    }

    private static void StudentT_ZeroStatistic_ReturnsOneAndIsSymmetric()
    {
        Check.Near(1.0, SeriesMath.StudentTTwoSidedPValue(0.0, 5), 1e-12);
        Check.Near(
            SeriesMath.StudentTTwoSidedPValue(2.0, 10),
            SeriesMath.StudentTTwoSidedPValue(-2.0, 10),
            1e-12);
    }

    private static void IncompleteBeta_KnownIdentities_Hold()
    {
        // I_x(1, 1) = x; I_0.5(a, a) = 0.5 by symmetry.
        Check.Near(0.3, SeriesMath.RegularizedIncompleteBeta(0.3, 1.0, 1.0), 1e-12);
        Check.Near(0.5, SeriesMath.RegularizedIncompleteBeta(0.5, 2.0, 2.0), 1e-12);
        Check.Near(0.0, SeriesMath.RegularizedIncompleteBeta(0.0, 2.0, 3.0), 0.0);
        Check.Near(1.0, SeriesMath.RegularizedIncompleteBeta(1.0, 2.0, 3.0), 0.0);
    }

    private static void LogGamma_KnownFactorials_Hold()
    {
        // Gamma(n) = (n - 1)!
        Check.Near(Math.Log(24.0), SeriesMath.LogGamma(5.0), 1e-10);
        Check.Near(Math.Log(1.0), SeriesMath.LogGamma(1.0), 1e-10);
        Check.Near(Math.Log(Math.Sqrt(Math.PI)), SeriesMath.LogGamma(0.5), 1e-10);
    }

    private static void Spike_InjectedSpikeInFlatSeries_IsFlaggedCritical()
    {
        const int n = 60;
        long[] times = MakeTimes(n);
        double[] values = MakeWobble(n);
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

    private static void Spike_SmoothSeries_IsNotFlagged()
    {
        const int n = 120;
        long[] times = MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 100.0 + 10.0 * Math.Sin(i / 20.0);
        }

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(0, findings.Count);
    }

    private static void Spike_ThreeSampleRun_CollapsesToOneFinding()
    {
        const int n = 60;
        long[] times = MakeTimes(n);
        double[] values = MakeWobble(n);
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

    private static void Spike_SeparatedSpikes_AreReportedSeparately()
    {
        const int n = 80;
        long[] times = MakeTimes(n);
        double[] values = MakeWobble(n);
        values[30] = 1000.0;
        values[60] = 1000.0;

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(2, findings.Count);
        Check.Equal(times[30], findings[0].At);
        Check.Equal(times[60], findings[1].At);
    }

    private static void Spike_ManySpikes_CapsAtTwentyKeepingMostRecent()
    {
        // 25 spikes spaced 40 samples apart: each baseline is clean, so all 25 are candidates.
        const int n = 1000;
        long[] times = MakeTimes(n);
        double[] values = MakeWobble(n);
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

    private static void Spike_NaNSample_IsSkippedAndDoesNotBreakScoring()
    {
        const int n = 60;
        long[] times = MakeTimes(n);
        double[] values = MakeWobble(n);
        values[45] = double.NaN;
        values[46] = 1000.0;

        List<DiagnosticFinding> findings = new();
        new SpikeDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(times[46], findings[0].At);
    }

    private static void Trend_SteadyRise_IsFlagged()
    {
        const int n = 60;
        long[] times = MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + 5.0 * i + Wobble(i);
        }

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(FindingKind.Trend, findings[0].Kind);
        Check.Near(300.0, findings[0].Score, 5.0);
        Check.Equal(times[n - 1], findings[0].At);
        Check.True(findings[0].Message.StartsWith("Rising +", StringComparison.Ordinal), $"unexpected message: {findings[0].Message}");
    }

    private static void Trend_FlatNoisySeries_IsNotFlagged()
    {
        const int n = 60;
        long[] times = MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + ((i * 37) % 11 - 5) * 3.0;
        }

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(0, findings.Count);
    }

    private static void Trend_NaNPairsAreIgnored()
    {
        const int n = 60;
        long[] times = MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + 5.0 * i + Wobble(i);
        }

        values[10] = double.NaN;
        values[30] = double.NaN;

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(1, findings.Count);
        Check.Equal(times[n - 1], findings[0].At);
    }

    private static void Trend_TooFewSamples_IsNotFlagged()
    {
        const int n = 10;
        long[] times = MakeTimes(n);
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1000.0 + 50.0 * i;
        }

        List<DiagnosticFinding> findings = new();
        new TrendDetector().Detect("Test.Metric", times, values, findings);

        Check.Equal(0, findings.Count);
    }

    private static long[] MakeTimes(int n)
    {
        long[] times = new long[n];
        for (int i = 0; i < n; i++)
        {
            times[i] = BaseTimeMs + OneSecondMs * i;
        }

        return times;
    }

    private static double Wobble(int i)
    {
        return ((i * 7) % 5 - 2) * 0.5;
    }

    private static double[] MakeWobble(int n)
    {
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 100.0 + Wobble(i);
        }

        return values;
    }
}
