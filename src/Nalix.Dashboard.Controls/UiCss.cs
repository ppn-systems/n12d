// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Controls;

/// <summary>
/// Maps <see cref="UiTone"/> values to the CSS classes and colour tokens used by the controls.
/// </summary>
internal static class UiCss
{
    /// <summary>Returns the modifier class for a tone's status dot; empty for neutral.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>A CSS class name, or an empty string.</returns>
    public static string ToneClass(UiTone tone) => tone switch
    {
        UiTone.Accent => "ui-tone--accent",
        UiTone.Ok => "ui-tone--success",
        UiTone.Warn => "ui-tone--warning",
        UiTone.Danger => "ui-tone--danger",
        _ => string.Empty,
    };

    /// <summary>Returns the badge modifier class for a tone.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>A CSS class name.</returns>
    public static string BadgeClass(UiTone tone) => tone switch
    {
        UiTone.Accent => "ui-badge--accent",
        UiTone.Ok => "ui-badge--success",
        UiTone.Warn => "ui-badge--warning",
        UiTone.Danger => "ui-badge--danger",
        _ => "ui-badge--neutral",
    };

    /// <summary>Returns a CSS colour expression for a tone (a design token reference).</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>A <c>var(--…)</c> expression.</returns>
    public static string ToneColor(UiTone tone) => tone switch
    {
        UiTone.Accent => "var(--accent)",
        UiTone.Ok => "var(--success)",
        UiTone.Warn => "var(--warning)",
        UiTone.Danger => "var(--danger)",
        _ => "var(--text-muted)",
    };
}
