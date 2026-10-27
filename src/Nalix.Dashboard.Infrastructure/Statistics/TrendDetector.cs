// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Globalization;
using Nalix.Dashboard.Abstractions.Diagnostics;

namespace Nalix.Dashboard.Infrastructure.Statistics;

/// <summary>
/// Detects statistically significant, practically meaningful upward trends (for example memory or handle leaks)
/// with a least-squares fit of value over elapsed minutes.
/// </summary>
public sealed class TrendDetector
{
    private const double MillisecondsPerMinute = 60000.0;
    private const double CriticalRelativeGrowth = 0.5;
    private const double CriticalRSquared = 0.8;
    private const double WarningRSquared = 0.5;
    private const double VerySignificantPValue = 0.001;

    private readonly int _minSamples;
    private readonly double _maxPValue;
    private readonly double _minRelativeGrowth;

    /// <summary>
    /// Initializes a new trend detector.
    /// </summary>
    /// <param name="minSamples">The minimum number of valid samples required for a fit. Must be at least 3.</param>
    /// <param name="maxPValue">The significance level for the slope. Must be in (0, 1].</param>
    /// <param name="minRelativeGrowth">The minimum total growth over the window, as a fraction of |mean|. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter is outside its valid range.</exception>
    public TrendDetector(int minSamples = 30, double maxPValue = 0.05, double minRelativeGrowth = 0.05)
    {
        if (minSamples < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(minSamples), "minSamples must be at least 3.");
        }

        if (double.IsNaN(maxPValue) || maxPValue <= 0.0 || maxPValue > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPValue), "maxPValue must be in (0, 1].");
        }

        if (double.IsNaN(minRelativeGrowth) || minRelativeGrowth < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(minRelativeGrowth), "minRelativeGrowth must be non-negative.");
        }

        _minSamples = minSamples;
        _maxPValue = maxPValue;
        _minRelativeGrowth = minRelativeGrowth;
    }

    /// <summary>
    /// Fits the series and appends a <see cref="FindingKind.Trend"/> finding when it is a significant, meaningful rise.
    /// </summary>
    /// <param name="metric">The metric path that the finding refers to.</param>
    /// <param name="times">Unix-millisecond timestamps, one per sample.</param>
    /// <param name="values">The sample values. Pairs with a NaN value are ignored.</param>
    /// <param name="output">The list that receives the finding, if any.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="times"/> and <paramref name="values"/> differ in length.</exception>
    public void Detect(string metric, ReadOnlySpan<long> times, ReadOnlySpan<double> values, List<DiagnosticFinding> output)
    {
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(output);

        if (times.Length != values.Length)
        {
            throw new ArgumentException("times and values must have the same length.", nameof(values));
        }

        int n = values.Length;
        double[] xs = ArrayPool<double>.Shared.Rent(n);
        double[] ys = ArrayPool<double>.Shared.Rent(n);
        try
        {
            int count = 0;
            long firstTime = 0;
            long lastTime = 0;

            for (int i = 0; i < n; i++)
            {
                if (double.IsNaN(values[i]))
                {
                    continue;
                }

                if (count == 0)
                {
                    firstTime = times[i];
                }

                xs[count] = (times[i] - firstTime) / MillisecondsPerMinute;
                ys[count] = values[i];
                lastTime = times[i];
                count++;
            }

            if (count < _minSamples)
            {
                return;
            }

            ReadOnlySpan<double> x = xs.AsSpan(0, count);
            ReadOnlySpan<double> y = ys.AsSpan(0, count);
            LinearFit fit = SeriesMath.Fit(x, y);

            // NaN-safe: a NaN slope or p-value fails both comparisons and is not reported.
            if (!(fit.Slope > 0.0) || !(fit.PValue < _maxPValue))
            {
                return;
            }

            double durationMinutes = x[count - 1] - x[0];
            double growth = fit.Slope * durationMinutes;
            double absMean = Math.Abs(SeriesMath.Mean(y));

            bool meaningful = absMean == 0.0
                ? growth > 0.0
                : growth >= _minRelativeGrowth * absMean;
            if (!meaningful)
            {
                return;
            }

            double relativeGrowth = absMean == 0.0 ? double.PositiveInfinity : growth / absMean;
            FindingSeverity severity;
            if (relativeGrowth >= CriticalRelativeGrowth && fit.RSquared >= CriticalRSquared)
            {
                severity = FindingSeverity.Critical;
            }
            else if (fit.RSquared >= WarningRSquared)
            {
                severity = FindingSeverity.Warning;
            }
            else
            {
                severity = FindingSeverity.Info;
            }

            output.Add(new DiagnosticFinding(
                FindingKind.Trend,
                severity,
                metric,
                CreateMessage(fit, durationMinutes),
                fit.Slope,
                lastTime));
        }
        finally
        {
            ArrayPool<double>.Shared.Return(xs);
            ArrayPool<double>.Shared.Return(ys);
        }
    }

    private static string CreateMessage(LinearFit fit, double durationMinutes)
    {
        string pText = fit.PValue < VerySignificantPValue
            ? "p < 0.001"
            : "p = " + fit.PValue.ToString("0.000", CultureInfo.InvariantCulture);

        return string.Format(
            CultureInfo.InvariantCulture,
            "Rising {0}/min over {1} min (R² {2}, {3})",
            fit.Slope.ToString("+#,0.###;-#,0.###", CultureInfo.InvariantCulture),
            durationMinutes.ToString("0.#", CultureInfo.InvariantCulture),
            fit.RSquared.ToString("0.00", CultureInfo.InvariantCulture),
            pText);
    }
}
