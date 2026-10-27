// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Nalix.Dashboard.Infrastructure.Export;

namespace Nalix.Dashboard.Tests;

/// <summary>
/// Tests for <see cref="CsvBuilder"/>.
/// </summary>
internal static class CsvTests
{
    /// <summary>
    /// Registers every CSV test with the runner.
    /// </summary>
    /// <param name="tests">The runner's test list.</param>
    public static void Register(List<(string Name, Action Body)> tests)
    {
        tests.Add(("Csv_HeaderAndRow_AreWrittenInOrder", Csv_HeaderAndRow_AreWrittenInOrder));
        tests.Add(("Csv_HeaderCellsWithSpecialCharacters_AreQuoted", Csv_HeaderCellsWithSpecialCharacters_AreQuoted));
        tests.Add(("Csv_NaNValue_IsEmptyCell", Csv_NaNValue_IsEmptyCell));
        tests.Add(("Csv_Timestamp_IsIsoUtcWithMilliseconds", Csv_Timestamp_IsIsoUtcWithMilliseconds));
        tests.Add(("Csv_ColumnCountMismatch_Throws", Csv_ColumnCountMismatch_Throws));
        tests.Add(("Csv_Output_HasNoByteOrderMark", Csv_Output_HasNoByteOrderMark));
        tests.Add(("Csv_EmptyBuilder_WritesHeaderOnly", Csv_EmptyBuilder_WritesHeaderOnly));
    }

    private static void Csv_HeaderAndRow_AreWrittenInOrder()
    {
        CsvBuilder builder = new(["cpu", "memory"]);
        builder.AddRow(0L, [1.5, 2.0]);

        Check.Equal("timestamp_utc,cpu,memory\n1970-01-01T00:00:00.000Z,1.5,2\n", Text(builder));
        Check.Equal(1, builder.RowCount);
    }

    private static void Csv_HeaderCellsWithSpecialCharacters_AreQuoted()
    {
        CsvBuilder builder = new(["a,b", "say \"hi\"", "line\nbreak"]);

        Check.Equal("timestamp_utc,\"a,b\",\"say \"\"hi\"\"\",\"line\nbreak\"\n", Text(builder));
    }

    private static void Csv_NaNValue_IsEmptyCell()
    {
        CsvBuilder builder = new(["a", "b", "c"]);
        builder.AddRow(0L, [double.NaN, 2.5, double.NaN]);

        Check.Equal("timestamp_utc,a,b,c\n1970-01-01T00:00:00.000Z,,2.5,\n", Text(builder));
    }

    private static void Csv_Timestamp_IsIsoUtcWithMilliseconds()
    {
        long unixMs = new DateTimeOffset(2026, 10, 27, 1, 2, 3, 456, TimeSpan.Zero).ToUnixTimeMilliseconds();
        CsvBuilder builder = new(["v"]);
        builder.AddRow(unixMs, [1.0]);

        string text = Text(builder);
        Check.True(text.EndsWith("2026-10-27T01:02:03.456Z,1\n", StringComparison.Ordinal), $"unexpected row: {text}");
    }

    private static void Csv_ColumnCountMismatch_Throws()
    {
        CsvBuilder builder = new(["a", "b"]);
        Check.Throws<ArgumentException>(() => builder.AddRow(0L, [1.0]));
        Check.Throws<ArgumentException>(() => builder.AddRow(0L, [1.0, 2.0, 3.0]));
        Check.Equal(0, builder.RowCount);
    }

    private static void Csv_Output_HasNoByteOrderMark()
    {
        CsvBuilder builder = new(["a"]);
        byte[] bytes = builder.ToUtf8();

        Check.True(bytes.Length > 0 && bytes[0] == (byte)'t', "output must start with the header, not a BOM");
    }

    private static void Csv_EmptyBuilder_WritesHeaderOnly()
    {
        CsvBuilder builder = new(["a"]);

        Check.Equal("timestamp_utc,a\n", Text(builder));
        Check.Equal(0, builder.RowCount);
    }

    private static string Text(CsvBuilder builder)
    {
        return Encoding.UTF8.GetString(builder.ToUtf8());
    }
}
