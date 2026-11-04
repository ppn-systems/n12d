// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Infrastructure.Statistics;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.Infrastructure.Statistics;

/// <summary>
/// Tests for <see cref="SeriesMath"/>.
/// </summary>
public sealed class SeriesMathTests
{
    [Test]
    public void Mean_KnownData_ReturnsAverageAndNaNForEmpty()
    {
        Check.Near(2.5, SeriesMath.Mean([1.0, 2.0, 3.0, 4.0]), 1e-12);
        Check.True(double.IsNaN(SeriesMath.Mean(ReadOnlySpan<double>.Empty)), "mean of empty must be NaN");
    }

    [Test]
    public void StandardDeviation_KnownData_UsesSampleDenominator()
    {
        // Mean 5, sum of squared deviations 32, sample variance 32 / 7.
        Check.Near(Math.Sqrt(32.0 / 7.0), SeriesMath.StandardDeviation([2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0]), 1e-12);
        Check.Near(0.0, SeriesMath.StandardDeviation([5.0]), 0.0);
    }

    [Test]
    public void ZScore_KnownWindow_ReturnsStandardisedDistance()
    {
        // Window [1..5]: mean 3, sample standard deviation sqrt(2.5).
        double sd = Math.Sqrt(2.5);
        Check.Near(2.0, SeriesMath.ZScore([1.0, 2.0, 3.0, 4.0, 5.0], 3.0 + 2.0 * sd), 1e-9);
        Check.Near(-1.0, SeriesMath.ZScore([1.0, 2.0, 3.0, 4.0, 5.0], 3.0 - sd), 1e-9);
    }

    [Test]
    public void ZScore_ZeroSpreadEqualValue_ReturnsZero()
    {
        Check.Near(0.0, SeriesMath.ZScore([5.0, 5.0, 5.0], 5.0), 0.0);
    }

    [Test]
    public void ZScore_ZeroSpreadDifferentValue_ReturnsSentinel()
    {
        Check.Near(SeriesMath.InfiniteZScore, SeriesMath.ZScore([5.0, 5.0, 5.0], 6.0), 0.0);
        Check.Near(SeriesMath.InfiniteZScore, SeriesMath.ZScore([5.0, 5.0, 5.0], 4.0), 0.0);
        Check.Near(99.0, SeriesMath.InfiniteZScore, 0.0);
    }

    [Test]
    public void ZScore_ConstantWindowWithRoundingNoise_ReturnsZero()
    {
        // Summing 0.1 seven times and dividing does not return exactly 0.1; the zero-spread rule must still apply.
        double[] window = [0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1];
        Check.Near(0.0, SeriesMath.ZScore(window, 0.1), 0.0);
    }

    [Test]
    public void Fit_ExactLine_RecoversSlopeInterceptAndPerfectFit()
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

    [Test]
    public void Fit_SmallNoisySample_MatchesHandComputedLeastSquares()
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

    [Test]
    public void Fit_LongDeterministicNoise_RecoversKnownSlope()
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

    [Test]
    public void Fit_ConstantX_ReturnsZeroSlopeAndPValueOne()
    {
        LinearFit fit = SeriesMath.Fit([3.0, 3.0, 3.0, 3.0], [1.0, 2.0, 3.0, 4.0]);
        Check.Near(0.0, fit.Slope, 0.0);
        Check.Near(1.0, fit.PValue, 0.0);
    }

    [Test]
    public void Fit_FewerThanThreePoints_ReturnsPValueOne()
    {
        LinearFit fit = SeriesMath.Fit([0.0, 1.0], [0.0, 1.0]);
        Check.Near(0.0, fit.Slope, 0.0);
        Check.Near(1.0, fit.PValue, 0.0);
        Check.Equal(2, fit.Count);
    }

    [Test]
    public void StudentTTwoSidedPValue_ReferenceValues_MatchTables()
    {
        Check.Near(0.0734, SeriesMath.StudentTTwoSidedPValue(2.0, 10), 0.0005);
        Check.Near(0.05, SeriesMath.StudentTTwoSidedPValue(12.706, 1), 0.001);
        Check.Near(0.0503, SeriesMath.StudentTTwoSidedPValue(1.96, 1000), 0.001);
    }

    [Test]
    public void StudentTTwoSidedPValue_ZeroStatistic_ReturnsOneAndIsSymmetric()
    {
        Check.Near(1.0, SeriesMath.StudentTTwoSidedPValue(0.0, 5), 1e-12);
        Check.Near(
            SeriesMath.StudentTTwoSidedPValue(2.0, 10),
            SeriesMath.StudentTTwoSidedPValue(-2.0, 10),
            1e-12);
    }

    [Test]
    public void RegularizedIncompleteBeta_KnownIdentities_ReturnsIdentityValues()
    {
        // I_x(1, 1) = x; I_0.5(a, a) = 0.5 by symmetry.
        Check.Near(0.3, SeriesMath.RegularizedIncompleteBeta(0.3, 1.0, 1.0), 1e-12);
        Check.Near(0.5, SeriesMath.RegularizedIncompleteBeta(0.5, 2.0, 2.0), 1e-12);
        Check.Near(0.0, SeriesMath.RegularizedIncompleteBeta(0.0, 2.0, 3.0), 0.0);
        Check.Near(1.0, SeriesMath.RegularizedIncompleteBeta(1.0, 2.0, 3.0), 0.0);
    }

    [Test]
    public void LogGamma_KnownFactorials_ReturnsLogOfFactorials()
    {
        // Gamma(n) = (n - 1)!
        Check.Near(Math.Log(24.0), SeriesMath.LogGamma(5.0), 1e-10);
        Check.Near(Math.Log(1.0), SeriesMath.LogGamma(1.0), 1e-10);
        Check.Near(Math.Log(Math.Sqrt(Math.PI)), SeriesMath.LogGamma(0.5), 1e-10);
    }
}
