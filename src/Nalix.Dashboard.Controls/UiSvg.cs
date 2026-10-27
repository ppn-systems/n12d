// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;

namespace Nalix.Dashboard.Controls;

/// <summary>
/// Allocation-free helpers that write SVG coordinates into a <see cref="StringBuilder"/>.
/// </summary>
internal static class UiSvg
{
    private const string CoordinateFormat = "0.0";

    /// <summary>Appends a coordinate with one decimal place, using invariant culture.</summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="value">A finite coordinate.</param>
    public static void AppendCoordinate(StringBuilder builder, double value)
    {
        Span<char> buffer = stackalloc char[32];
        if (value.TryFormat(buffer, out int written, CoordinateFormat, CultureInfo.InvariantCulture))
        {
            builder.Append(buffer[..written]);
        }
        else
        {
            builder.Append(value.ToString(CoordinateFormat, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Appends an <c>x,y</c> pair.</summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public static void AppendPoint(StringBuilder builder, double x, double y)
    {
        AppendCoordinate(builder, x);
        builder.Append(',');
        AppendCoordinate(builder, y);
    }
}
