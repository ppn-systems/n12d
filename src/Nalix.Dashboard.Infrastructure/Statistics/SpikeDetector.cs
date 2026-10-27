// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Globalization;
using Nalix.Dashboard.Abstractions.Diagnostics;

namespace Nalix.Dashboard.Infrastructure.Statistics;

/// <summary>
/// Flags samples that deviate sharply from the rolling baseline of the samples immediately before them.
/// </summary>
/// <remarks>
/// The baseline is shifted by one sample, so a spike never masks itself. Consecutive flagged samples collapse into
/// one finding that reports the strongest sample of the run. At most 20 findings are reported per call.
/// </remarks>
public sealed class SpikeDetector
{
    private const int MaxFindingsPerCall = 20;

    private readonly int _window;
    private readonly double _threshold;
    private readonly int _minSamples;

    /// <summary>
    /// Initializes a new spike detector.
    /// </summary>
    /// <param name="window">The number of preceding samples in the baseline. Must be at least 2.</param>
    /// <param name="threshold">The |z| at which a sample is flagged. Must be positive.</param>
    /// <param name="minSamples">The minimum number of samples before scoring starts. Must be between 2 and <paramref name="window"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a parameter is outside its valid range.</exception>
    public SpikeDetector(int window = 30, double threshold = 3.0, int minSamples = 12)
    {
        if (window < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be at least 2.");
        }

        if (double.IsNaN(threshold) || threshold <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be positive.");
        }

        if (minSamples < 2 || minSamples > window)
        {
            throw new ArgumentOutOfRangeException(nameof(minSamples), "minSamples must be between 2 and window.");
        }

        _window = window;
        _threshold = threshold;
        _minSamples = minSamples;
    }

    /// <summary>
    /// Scans a series and appends a <see cref="FindingKind.Spike"/> finding for each anomalous run.
    /// </summary>
    /// <param name="metric">The metric path that the findings refer to.</param>
    /// <param name="times">Unix-millisecond timestamps, one per sample.</param>
    /// <param name="values">The sample values. NaN samples are skipped.</param>
    /// <param name="output">The list that receives the findings.</param>
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
        if (n <= _minSamples)
        {
            return;
        }

        List<DiagnosticFinding> found = new();
        double[] scratch = ArrayPool<double>.Shared.Rent(_window);
        try
        {
            Candidate? run = null;

            for (int i = _minSamples; i < n; i++)
            {
                double value = values[i];
                int count = 0;

                if (!double.IsNaN(value))
                {
                    int start = Math.Max(0, i - _window);
                    for (int j = start; j < i; j++)
                    {
                        if (!double.IsNaN(values[j]))
                        {
                            scratch[count++] = values[j];
                        }
                    }
                }

                bool spike = false;
                Candidate current = default;

                if (count >= _minSamples)
                {
                    ReadOnlySpan<double> baseline = scratch.AsSpan(0, count);
                    double z = SeriesMath.ZScore(baseline, value);
                    if (Math.Abs(z) >= _threshold)
                    {
                        spike = true;
                        current = new Candidate(
                            times[i],
                            value,
                            SeriesMath.Mean(baseline),
                            SeriesMath.StandardDeviation(baseline),
                            z);
                    }
                }

                if (spike)
                {
                    if (run is null || Math.Abs(current.Z) > Math.Abs(run.Value.Z))
                    {
                        run = current;
                    }
                }
                else if (run is not null)
                {
                    found.Add(CreateFinding(metric, run.Value, _threshold));
                    run = null;
                }
            }

            if (run is not null)
            {
                found.Add(CreateFinding(metric, run.Value, _threshold));
            }
        }
        finally
        {
            ArrayPool<double>.Shared.Return(scratch);
        }

        int first = Math.Max(0, found.Count - MaxFindingsPerCall);
        for (int k = first; k < found.Count; k++)
        {
            output.Add(found[k]);
        }
    }

    private static DiagnosticFinding CreateFinding(string metric, Candidate spike, double threshold)
    {
        double absZ = Math.Abs(spike.Z);
        FindingSeverity severity = absZ >= 2.0 * threshold ? FindingSeverity.Critical : FindingSeverity.Warning;
        string zText = absZ == SeriesMath.InfiniteZScore
            ? "inf"
            : spike.Z.ToString("0.0", CultureInfo.InvariantCulture);

        string message = string.Format(
            CultureInfo.InvariantCulture,
            "Spike: {0} vs baseline {1} ± {2} (z = {3})",
            Format(spike.Value),
            Format(spike.Mean),
            Format(spike.StdDev),
            zText);

        return new DiagnosticFinding(FindingKind.Spike, severity, metric, message, absZ, spike.At);
    }

    private static string Format(double value)
    {
        return value.ToString("#,0.##", CultureInfo.InvariantCulture);
    }

    private readonly record struct Candidate(long At, double Value, double Mean, double StdDev, double Z);
}
