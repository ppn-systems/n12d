// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace Nalix.Dashboard.Controls;

/// <summary>
/// Formats report values for display. Output is culture-invariant; non-finite numbers render as an em dash.
/// </summary>
public static class UiFormat
{
    /// <summary>The placeholder for missing or non-finite values.</summary>
    public const string Dash = "—";

    private const double Kibi = 1024d;
    private const double MillisecondsPerMinute = 60000d;
    private const double MillisecondsPerHour = 3600000d;
    private const double MillisecondsPerDay = 86400000d;

    // Keep one day of margin so that local-time conversion never leaves the DateTimeOffset range.
    private const long MinUnixMilliseconds = -62135596800000L + 86400000L;
    private const long MaxUnixMilliseconds = 253402300799999L - 86400000L;

    private static readonly string[] s_byteUnits = ["B", "KiB", "MiB", "GiB", "TiB"];

    /// <summary>Formats a number: integers with thousands separators, other values up to two decimals.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The formatted text, or an em dash for NaN or infinity.</returns>
    public static string Number(double value)
    {
        if (!double.IsFinite(value))
        {
            return Dash;
        }

        double rounded = Math.Round(value);
        if (Math.Abs(value - rounded) < 1e-9)
        {
            return NoNegativeZero(rounded).ToString("#,0", CultureInfo.InvariantCulture);
        }

        return value.ToString("#,0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats a byte count with binary units (B, KiB, MiB, GiB, TiB).</summary>
    /// <param name="value">The number of bytes.</param>
    /// <returns>The formatted text, for example <c>1.5 MiB</c>.</returns>
    public static string Bytes(double value)
    {
        if (!double.IsFinite(value))
        {
            return Dash;
        }

        double magnitude = Math.Abs(value);
        int unit = 0;
        while (magnitude >= Kibi && unit < s_byteUnits.Length - 1)
        {
            magnitude /= Kibi;
            unit++;
        }

        double signed = value < 0 ? -magnitude : magnitude;
        if (unit == 0)
        {
            return NoNegativeZero(Math.Round(signed)).ToString("#,0", CultureInfo.InvariantCulture) + " B";
        }

        return signed.ToString("0.0", CultureInfo.InvariantCulture) + " " + s_byteUnits[unit];
    }

    /// <summary>Formats a duration given in milliseconds.</summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <returns>For example <c>0.42 ms</c>, <c>12 ms</c>, <c>1.2 s</c>, <c>3m 4s</c>, <c>2h 5m</c> or <c>3d 4h</c>.</returns>
    public static string Duration(double milliseconds)
    {
        if (!double.IsFinite(milliseconds))
        {
            return Dash;
        }

        if (milliseconds < 0)
        {
            return "-" + Duration(-milliseconds);
        }

        if (milliseconds < 1)
        {
            return milliseconds.ToString("0.00", CultureInfo.InvariantCulture) + " ms";
        }

        double roundedMilliseconds = Math.Round(milliseconds);
        if (roundedMilliseconds < 1000)
        {
            return roundedMilliseconds.ToString("0", CultureInfo.InvariantCulture) + " ms";
        }

        if (milliseconds < MillisecondsPerMinute)
        {
            return (milliseconds / 1000d).ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }

        long totalSeconds = (long)Math.Round(milliseconds / 1000d);
        if (milliseconds < MillisecondsPerHour)
        {
            return Integer(totalSeconds / 60) + "m " + Integer(totalSeconds % 60) + "s";
        }

        if (milliseconds < MillisecondsPerDay)
        {
            return Integer(totalSeconds / 3600) + "h " + Integer(totalSeconds % 3600 / 60) + "m";
        }

        return Integer(totalSeconds / 86400) + "d " + Integer(totalSeconds % 86400 / 3600) + "h";
    }

    /// <summary>Formats a value that is already a percentage.</summary>
    /// <param name="value">The percentage, for example <c>12.3</c>.</param>
    /// <returns>For example <c>12.3%</c>.</returns>
    public static string Percent(double value)
    {
        if (!double.IsFinite(value))
        {
            return Dash;
        }

        return value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>Formats a per-second rate.</summary>
    /// <param name="perSecond">The rate.</param>
    /// <returns>For example <c>1,250/s</c>.</returns>
    public static string Rate(double perSecond)
    {
        if (!double.IsFinite(perSecond))
        {
            return Dash;
        }

        return Number(perSecond) + "/s";
    }

    /// <summary>Formats a value using the unit implied by its metric name.</summary>
    /// <param name="name">The metric name, for example <c>TotalBytesSent</c> or <c>LatencyMs</c>.</param>
    /// <param name="value">The value.</param>
    /// <returns>The formatted text.</returns>
    public static string Auto(string name, double value)
    {
        if (name.EndsWith("Bytes", StringComparison.Ordinal))
        {
            return Bytes(value);
        }

        if (name.EndsWith("MB", StringComparison.Ordinal))
        {
            return Bytes(value * Kibi * Kibi);
        }

        if (name.EndsWith("Ms", StringComparison.Ordinal))
        {
            return Duration(value);
        }

        if (name.EndsWith("Seconds", StringComparison.Ordinal))
        {
            return Duration(value * 1000d);
        }

        if (name.EndsWith("Days", StringComparison.Ordinal))
        {
            return Duration(value * MillisecondsPerDay);
        }

        if (name.Contains("Percent", StringComparison.Ordinal))
        {
            return Percent(value);
        }

        if (name.EndsWith("PerSecond", StringComparison.Ordinal))
        {
            return Rate(value);
        }

        return Number(value);
    }

    /// <summary>Formats a Unix millisecond timestamp as local time <c>HH:mm:ss</c>.</summary>
    /// <param name="unixMilliseconds">The timestamp.</param>
    /// <returns>The local time, or an em dash when out of range.</returns>
    public static string Time(long unixMilliseconds)
    {
        if (unixMilliseconds < MinUnixMilliseconds || unixMilliseconds > MaxUnixMilliseconds)
        {
            return Dash;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds)
            .ToLocalTime()
            .ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Turns a metric or property name into readable words: <c>TotalBytesSent</c> becomes
    /// <c>Total bytes sent</c>, <c>IPAddress</c> becomes <c>IP address</c>, <c>max_depth</c> becomes <c>Max depth</c>.
    /// </summary>
    /// <param name="name">The identifier.</param>
    /// <returns>The readable label.</returns>
    public static string Humanize(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        StringBuilder spaced = new(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            if (current == '_' || current == '-')
            {
                spaced.Append(' ');
                continue;
            }

            if (i > 0 && char.IsUpper(current))
            {
                char previous = name[i - 1];
                bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    spaced.Append(' ');
                }
            }

            spaced.Append(current);
        }

        string[] words = spaced.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // The unit suffix is dropped because Auto() already renders it with the value ("Uptime" → "20.6 s",
        // not "Uptime ms" → "20.6 s").
        int count = words.Length;
        if (count > 1 && s_unitSuffixes.Contains(words[count - 1]))
        {
            count--;
        }

        StringBuilder result = new(spaced.Length);
        for (int w = 0; w < count; w++)
        {
            if (w > 0)
            {
                result.Append(' ');
            }

            string word = words[w];
            result.Append(IsAcronym(word) ? word : s_acronyms.Contains(word) ? word.ToUpperInvariant() : word.ToLowerInvariant());
        }

        if (result.Length > 0)
        {
            result[0] = char.ToUpperInvariant(result[0]);
        }

        return result.ToString();
    }

    private static readonly HashSet<string> s_unitSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ms", "Bytes", "MB", "Seconds", "Days", "Percent",
    };

    private static readonly HashSet<string> s_acronyms = new(StringComparer.OrdinalIgnoreCase)
    {
        "Os", "Cpu", "Gc", "Utc", "Id", "Ip", "Tcp", "Udp", "Ttl", "Url", "Io", "Api", "Ws", "P95", "P99",
    };

    private static bool IsAcronym(string word)
    {
        if (word.Length < 2)
        {
            return false;
        }

        for (int i = 0; i < word.Length; i++)
        {
            if (!char.IsUpper(word[i]) && !char.IsDigit(word[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static double NoNegativeZero(double value) => value == 0 ? 0 : value;

    private static string Integer(long value) => value.ToString(CultureInfo.InvariantCulture);
}
