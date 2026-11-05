// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Controls;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.Controls;

/// <summary>
/// Tests for <see cref="UiFormat"/>.
/// </summary>
public sealed class UiFormatTests
{
    [Test]
    public void Auto_WhenNameIsByteRate_FormatsBytesPerSecond()
    {
        string text = UiFormat.Auto("AllocationBytesPerSecond", 6144);

        Check.Equal("6.0 KiB/s", text);
    }

    [Test]
    public void Auto_WhenNameIsCountRate_FormatsBareRate()
    {
        string text = UiFormat.Auto("PacketsPerSecond", 12);

        Check.Equal("12/s", text);
    }

    [Test]
    public void Text_WhenDefaultTimestamp_ReturnsDash()
    {
        string text = UiFormat.Text("0001-01-01T00:00:00+00:00");

        Check.Equal(UiFormat.Dash, text);
    }

    [Test]
    public void Text_WhenTimestampIsOnAnotherDay_ReturnsLocalDateAndTime()
    {
        DateTimeOffset at = new(2020, 3, 4, 5, 6, 7, TimeSpan.Zero);

        string text = UiFormat.Text(at.ToString("O", System.Globalization.CultureInfo.InvariantCulture));

        Check.Equal(at.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), text);
    }

    [Test]
    public void Text_WhenPlainString_ReturnsItUnchanged()
    {
        string text = UiFormat.Text("dispatch.proc.0");

        Check.Equal("dispatch.proc.0", text);
    }

    [Test]
    public void Humanize_WhenNameEndsWithUnit_DropsUnitAndUppercasesAcronyms()
    {
        string text = UiFormat.Humanize("GcPauseTimeMs");

        Check.Equal("GC pause time", text);
    }
}
