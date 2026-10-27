// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Infrastructure.Statistics;

/// <summary>
/// Allocation-free descriptive statistics, least-squares regression and Student-t p-values over spans.
/// </summary>
public static class SeriesMath
{
    /// <summary>The z-score returned when the baseline has no spread and the value differs from it.</summary>
    public const double InfiniteZScore = 99.0;

    private const int MaxContinuedFractionIterations = 300;
    private const double ContinuedFractionEpsilon = 3.0e-16;
    private const double TinyFloat = 1.0e-300;

    // Relative tolerance used to recognise zero spread despite floating-point rounding noise.
    private const double RelativeZeroTolerance = 1.0e-12;

    // Series coefficients for ln Gamma (Numerical Recipes gammln); absolute error below 1e-9 for x > 0.
    private const double GammaSeriesConstant = 1.000000000190015;
    private const double SqrtTwoPi = 2.5066282746310005;
    private static readonly double[] s_gammaCoefficients =
    [
        76.18009172947146,
        -86.50532032941677,
        24.01409824083091,
        -1.231739572450155,
        0.1208650973866179e-2,
        -0.5395239384953e-5
    ];

    /// <summary>
    /// Computes the arithmetic mean.
    /// </summary>
    /// <param name="values">The samples.</param>
    /// <returns>The mean, or <see cref="double.NaN"/> when <paramref name="values"/> is empty.</returns>
    public static double Mean(ReadOnlySpan<double> values)
    {
        if (values.Length == 0)
        {
            return double.NaN;
        }

        double sum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum / values.Length;
    }

    /// <summary>
    /// Computes the sample standard deviation (n − 1 denominator).
    /// </summary>
    /// <param name="values">The samples.</param>
    /// <returns>The sample standard deviation, or 0 when there are fewer than two samples.</returns>
    public static double StandardDeviation(ReadOnlySpan<double> values)
    {
        int n = values.Length;
        if (n < 2)
        {
            return 0.0;
        }

        double mean = Mean(values);
        double sumOfSquares = 0.0;
        for (int i = 0; i < n; i++)
        {
            double deviation = values[i] - mean;
            sumOfSquares += deviation * deviation;
        }

        return Math.Sqrt(sumOfSquares / (n - 1));
    }

    /// <summary>
    /// Computes how many baseline standard deviations <paramref name="value"/> lies from the window mean.
    /// </summary>
    /// <param name="window">The baseline. It must not contain <paramref name="value"/>.</param>
    /// <param name="value">The sample being scored.</param>
    /// <returns>
    /// The z-score. When the window has no spread, 0 is returned if the value equals the mean (within rounding
    /// noise), otherwise <see cref="InfiniteZScore"/>. An empty window yields 0 (no baseline to judge against).
    /// </returns>
    public static double ZScore(ReadOnlySpan<double> window, double value)
    {
        if (window.Length == 0)
        {
            return 0.0;
        }

        double mean = Mean(window);
        double standardDeviation = StandardDeviation(window);
        if (standardDeviation <= RelativeZeroTolerance * Math.Max(1.0, Math.Abs(mean)))
        {
            return Math.Abs(value - mean) <= RelativeZeroTolerance * Math.Max(1.0, Math.Abs(mean))
                ? 0.0
                : InfiniteZScore;
        }

        return (value - mean) / standardDeviation;
    }

    /// <summary>
    /// Fits <c>y = Intercept + Slope * x</c> by ordinary least squares and tests whether the slope differs from zero.
    /// </summary>
    /// <param name="x">The independent variable.</param>
    /// <param name="y">The dependent variable; must have the same length as <paramref name="x"/>.</param>
    /// <returns>
    /// The fit. With fewer than three points, or with a constant <paramref name="x"/>, the slope is 0 and the
    /// p-value is 1. A perfect fit with a non-zero slope has a p-value of 0.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the two spans differ in length.</exception>
    public static LinearFit Fit(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException("x and y must have the same length.", nameof(y));
        }

        int n = x.Length;
        double meanY = Mean(y);
        if (n < 3)
        {
            return new LinearFit(0.0, meanY, 0.0, 1.0, n);
        }

        double meanX = Mean(x);
        double sxx = 0.0;
        double sxy = 0.0;
        double syy = 0.0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            sxx += dx * dx;
            sxy += dx * dy;
            syy += dy * dy;
        }

        double xScale = Math.Max(1.0, Math.Abs(meanX));
        if (sxx <= RelativeZeroTolerance * RelativeZeroTolerance * xScale * xScale * n)
        {
            return new LinearFit(0.0, meanY, 0.0, 1.0, n);
        }

        double slope = sxy / sxx;
        double intercept = meanY - slope * meanX;

        double ssResidual = 0.0;
        for (int i = 0; i < n; i++)
        {
            double residual = y[i] - (intercept + slope * x[i]);
            ssResidual += residual * residual;
        }

        bool perfect = ssResidual <= RelativeZeroTolerance * syy;

        double rSquared = 0.0;
        if (syy > 0.0)
        {
            rSquared = perfect ? 1.0 : Math.Clamp(1.0 - ssResidual / syy, 0.0, 1.0);
        }

        double pValue;
        if (slope == 0.0)
        {
            pValue = 1.0;
        }
        else if (perfect)
        {
            pValue = 0.0;
        }
        else
        {
            int degreesOfFreedom = n - 2;
            double standardError = Math.Sqrt(ssResidual / degreesOfFreedom / sxx);
            pValue = StudentTTwoSidedPValue(slope / standardError, degreesOfFreedom);
        }

        return new LinearFit(slope, intercept, rSquared, pValue, n);
    }

    /// <summary>
    /// Computes the two-sided p-value of a Student-t statistic, P(|T| ≥ |t|), exactly via the regularized incomplete beta function.
    /// </summary>
    /// <param name="t">The t statistic.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom; must be at least 1.</param>
    /// <returns>A value in [0, 1], or <see cref="double.NaN"/> when <paramref name="t"/> is NaN.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="degreesOfFreedom"/> is below 1.</exception>
    public static double StudentTTwoSidedPValue(double t, int degreesOfFreedom)
    {
        if (degreesOfFreedom < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom), "Degrees of freedom must be at least 1.");
        }

        if (double.IsNaN(t))
        {
            return double.NaN;
        }

        if (double.IsInfinity(t))
        {
            return 0.0;
        }

        double df = degreesOfFreedom;
        double x = df / (df + t * t);
        return RegularizedIncompleteBeta(x, df / 2.0, 0.5);
    }

    /// <summary>
    /// Computes the regularized incomplete beta function I_x(a, b) using a continued fraction (modified Lentz).
    /// </summary>
    internal static double RegularizedIncompleteBeta(double x, double a, double b)
    {
        if (x <= 0.0)
        {
            return 0.0;
        }

        if (x >= 1.0)
        {
            return 1.0;
        }

        double logFront = LogGamma(a + b) - LogGamma(a) - LogGamma(b)
                          + a * Math.Log(x) + b * Math.Log(1.0 - x);
        double front = Math.Exp(logFront);

        if (x < (a + 1.0) / (a + b + 2.0))
        {
            return front * BetaContinuedFraction(x, a, b) / a;
        }

        return 1.0 - front * BetaContinuedFraction(1.0 - x, b, a) / b;
    }

    /// <summary>
    /// Computes ln Γ(x) for x &gt; 0 with a six-term series.
    /// </summary>
    internal static double LogGamma(double x)
    {
        double y = x;
        double tmp = x + 5.5;
        tmp -= (x + 0.5) * Math.Log(tmp);

        double series = GammaSeriesConstant;
        for (int i = 0; i < s_gammaCoefficients.Length; i++)
        {
            y += 1.0;
            series += s_gammaCoefficients[i] / y;
        }

        return -tmp + Math.Log(SqrtTwoPi * series / x);
    }

    private static double BetaContinuedFraction(double x, double a, double b)
    {
        double qab = a + b;
        double qap = a + 1.0;
        double qam = a - 1.0;

        double c = 1.0;
        double d = Guard(1.0 - qab * x / qap);
        d = 1.0 / d;
        double h = d;

        for (int m = 1; m <= MaxContinuedFractionIterations; m++)
        {
            int m2 = 2 * m;

            double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = Guard(1.0 + aa * d);
            c = Guard(1.0 + aa / c);
            d = 1.0 / d;
            h *= d * c;

            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = Guard(1.0 + aa * d);
            c = Guard(1.0 + aa / c);
            d = 1.0 / d;
            double delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1.0) < ContinuedFractionEpsilon)
            {
                break;
            }
        }

        return h;
    }

    private static double Guard(double value)
    {
        return Math.Abs(value) < TinyFloat ? TinyFloat : value;
    }
}
