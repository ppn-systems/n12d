// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Tests.TestSupport;

/// <summary>
/// Deterministic time series builders shared by the statistics and detector tests.
/// </summary>
internal static class SeriesFixtures
{
    private const long BaseTimeMs = 1_700_000_000_000L;
    private const long OneSecondMs = 1000L;

    /// <summary>
    /// Builds <paramref name="n"/> timestamps spaced one second apart from a fixed base time.
    /// </summary>
    public static long[] MakeTimes(int n)
    {
        long[] times = new long[n];
        for (int i = 0; i < n; i++)
        {
            times[i] = BaseTimeMs + OneSecondMs * i;
        }

        return times;
    }

    /// <summary>
    /// Returns a small periodic offset in [-1, 1] that depends only on <paramref name="i"/>.
    /// </summary>
    public static double Wobble(int i)
    {
        return ((i * 7) % 5 - 2) * 0.5;
    }

    /// <summary>
    /// Builds <paramref name="n"/> samples around 100 with the <see cref="Wobble"/> offset applied.
    /// </summary>
    public static double[] MakeWobble(int n)
    {
        double[] values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = 100.0 + Wobble(i);
        }

        return values;
    }
}
