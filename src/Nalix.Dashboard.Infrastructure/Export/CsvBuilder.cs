// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace Nalix.Dashboard.Infrastructure.Export;

/// <summary>
/// Builds a UTF-8 (no BOM) CSV document with a UTC timestamp column followed by metric columns.
/// </summary>
/// <remarks>
/// Rows use <c>\n</c> line endings. Timestamps are ISO-8601 UTC with milliseconds. Numbers use the invariant
/// round-trip format; NaN is written as an empty cell.
/// </remarks>
public sealed class CsvBuilder
{
    private const string TimestampHeader = "timestamp_utc";
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private static readonly char[] s_headerSpecialCharacters = [',', '"', '\n', '\r'];

    private readonly string[] _columns;
    private readonly StringBuilder _rows = new();

    /// <summary>
    /// Initializes a new builder with the given metric columns.
    /// </summary>
    /// <param name="columns">The metric column names, excluding the timestamp column.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="columns"/> or one of its names is null.</exception>
    public CsvBuilder(IReadOnlyList<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        _columns = new string[columns.Count];
        for (int i = 0; i < columns.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(columns[i]);
            _columns[i] = columns[i];
        }
    }

    /// <summary>
    /// Gets the number of rows added so far.
    /// </summary>
    public int RowCount { get; private set; }

    /// <summary>
    /// Appends one row.
    /// </summary>
    /// <param name="unixMs">The sample time as Unix milliseconds (UTC).</param>
    /// <param name="values">One value per metric column, in column order. NaN is written as an empty cell.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> does not match the column count.</exception>
    public void AddRow(long unixMs, ReadOnlySpan<double> values)
    {
        if (values.Length != _columns.Length)
        {
            throw new ArgumentException(
                $"Expected {_columns.Length} values but received {values.Length}.",
                nameof(values));
        }

        DateTime utc = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
        _rows.Append(utc.ToString(TimestampFormat, CultureInfo.InvariantCulture));

        for (int i = 0; i < values.Length; i++)
        {
            _rows.Append(',');
            double value = values[i];
            if (!double.IsNaN(value))
            {
                _rows.Append(value.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        _rows.Append('\n');
        RowCount++;
    }

    /// <summary>
    /// Renders the header and all rows as UTF-8 bytes without a byte-order mark.
    /// </summary>
    /// <returns>The CSV document.</returns>
    public byte[] ToUtf8()
    {
        StringBuilder document = new();
        document.Append(TimestampHeader);
        for (int i = 0; i < _columns.Length; i++)
        {
            document.Append(',');
            AppendHeaderCell(document, _columns[i]);
        }

        document.Append('\n');
        document.Append(_rows);
        return Encoding.UTF8.GetBytes(document.ToString());
    }

    private static void AppendHeaderCell(StringBuilder document, string cell)
    {
        if (cell.IndexOfAny(s_headerSpecialCharacters) < 0)
        {
            document.Append(cell);
            return;
        }

        document.Append('"');
        document.Append(cell.Replace("\"", "\"\"", StringComparison.Ordinal));
        document.Append('"');
    }
}
