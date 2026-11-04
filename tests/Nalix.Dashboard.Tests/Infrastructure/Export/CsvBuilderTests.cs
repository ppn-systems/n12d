// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Nalix.Dashboard.Infrastructure.Export;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.Infrastructure.Export;

/// <summary>
/// Tests for <see cref="CsvBuilder"/>.
/// </summary>
public sealed class CsvBuilderTests
{
    [Test]
    public void AddRow_OneRow_WritesHeaderAndRowInOrder()
    {
        CsvBuilder builder = new(["cpu", "memory"]);
        builder.AddRow(0L, [1.5, 2.0]);

        Check.Equal("timestamp_utc,cpu,memory\n1970-01-01T00:00:00.000Z,1.5,2\n", Text(builder));
        Check.Equal(1, builder.RowCount);
    }

    [Test]
    public void ToUtf8_HeaderWithSpecialCharacters_QuotesCells()
    {
        CsvBuilder builder = new(["a,b", "say \"hi\"", "line\nbreak"]);

        Check.Equal("timestamp_utc,\"a,b\",\"say \"\"hi\"\"\",\"line\nbreak\"\n", Text(builder));
    }

    [Test]
    public void AddRow_NaNValue_WritesEmptyCell()
    {
        CsvBuilder builder = new(["a", "b", "c"]);
        builder.AddRow(0L, [double.NaN, 2.5, double.NaN]);

        Check.Equal("timestamp_utc,a,b,c\n1970-01-01T00:00:00.000Z,,2.5,\n", Text(builder));
    }

    [Test]
    public void AddRow_UnixTimestamp_WritesIsoUtcWithMilliseconds()
    {
        long unixMs = new DateTimeOffset(2026, 10, 27, 1, 2, 3, 456, TimeSpan.Zero).ToUnixTimeMilliseconds();
        CsvBuilder builder = new(["v"]);
        builder.AddRow(unixMs, [1.0]);

        string text = Text(builder);
        Check.True(text.EndsWith("2026-10-27T01:02:03.456Z,1\n", StringComparison.Ordinal), $"unexpected row: {text}");
    }

    [Test]
    public void AddRow_ColumnCountMismatch_ThrowsArgumentException()
    {
        CsvBuilder builder = new(["a", "b"]);
        Check.Throws<ArgumentException>(() => builder.AddRow(0L, [1.0]));
        Check.Throws<ArgumentException>(() => builder.AddRow(0L, [1.0, 2.0, 3.0]));
        Check.Equal(0, builder.RowCount);
    }

    [Test]
    public void ToUtf8_AnyRows_HasNoByteOrderMark()
    {
        CsvBuilder builder = new(["a"]);
        byte[] bytes = builder.ToUtf8();

        Check.True(bytes.Length > 0 && bytes[0] == (byte)'t', "output must start with the header, not a BOM");
    }

    [Test]
    public void ToUtf8_EmptyBuilder_WritesHeaderOnly()
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
