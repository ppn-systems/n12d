// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Infrastructure.Export;
using Nalix.Dashboard.State.Metrics;

namespace Nalix.Dashboard.State.Recording;

/// <summary>
/// Captures every metric value of every sample while recording, for CSV export.
/// </summary>
/// <remarks>
/// Columns are discovered as metrics appear; rows recorded before a column existed export it as empty.
/// </remarks>
public sealed class MetricRecorder
{
    /// <summary>The maximum rows kept (10 hours at one sample per second).</summary>
    public const int MaxRows = 36_000;

    private readonly Dictionary<string, int> _columns = new(StringComparer.Ordinal);
    private readonly List<string> _columnNames = [];
    private readonly List<(long Time, double[] Values)> _rows = [];

    /// <summary>Gets a value indicating whether samples are being captured.</summary>
    public bool IsRecording { get; private set; }

    /// <summary>Gets when the current or last recording started.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets the number of captured rows.</summary>
    public int RowCount => _rows.Count;

    /// <summary>Gets the number of captured columns.</summary>
    public int ColumnCount => _columnNames.Count;

    /// <summary>Starts a new recording, discarding the previous one.</summary>
    public void Start()
    {
        Clear();
        IsRecording = true;
        StartedAt = DateTimeOffset.Now;
    }

    /// <summary>Stops capturing; captured rows stay available for export.</summary>
    public void Stop() => IsRecording = false;

    /// <summary>Discards all captured rows and columns.</summary>
    public void Clear()
    {
        _columns.Clear();
        _columnNames.Clear();
        _rows.Clear();
        StartedAt = null;
    }

    /// <summary>
    /// Captures the values sampled at <paramref name="time"/> from <paramref name="history"/>.
    /// </summary>
    /// <param name="history">The metric history that has just recorded the sample.</param>
    /// <param name="time">The sample time.</param>
    public void Capture(MetricHistory history, long time)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (!IsRecording)
        {
            return;
        }

        if (_rows.Count >= MaxRows)
        {
            Stop();
            return;
        }

        foreach (MetricSeries series in history.Series.Values)
        {
            if (series.LatestTime == time && !_columns.ContainsKey(series.Path))
            {
                _columns.Add(series.Path, _columnNames.Count);
                _columnNames.Add(series.Path);
            }
        }

        double[] row = new double[_columnNames.Count];
        Array.Fill(row, double.NaN);
        foreach (MetricSeries series in history.Series.Values)
        {
            if (series.LatestTime == time && _columns.TryGetValue(series.Path, out int column))
            {
                row[column] = series.Latest;
            }
        }

        _rows.Add((time, row));
    }

    /// <summary>
    /// Renders the captured rows as UTF-8 CSV with columns sorted by path.
    /// </summary>
    /// <returns>The CSV bytes.</returns>
    public byte[] ExportCsv()
    {
        string[] sorted = [.. _columnNames];
        Array.Sort(sorted, StringComparer.Ordinal);

        int[] source = new int[sorted.Length];
        for (int i = 0; i < sorted.Length; i++)
        {
            source[i] = _columns[sorted[i]];
        }

        CsvBuilder csv = new(sorted);
        double[] buffer = new double[sorted.Length];
        foreach ((long time, double[] values) in _rows)
        {
            for (int i = 0; i < source.Length; i++)
            {
                buffer[i] = source[i] < values.Length ? values[source[i]] : double.NaN;
            }

            csv.AddRow(time, buffer);
        }

        return csv.ToUtf8();
    }
}
