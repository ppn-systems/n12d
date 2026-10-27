// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Controls;

/// <summary>
/// Maps <see cref="UiTone"/> values to the CSS classes and custom properties defined in the stylesheets.
/// </summary>
internal static class UiCss
{
    /// <summary>Returns the text colour class for a tone; empty for neutral.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>A CSS class name, or an empty string.</returns>
    public static string ToneClass(UiTone tone) => tone switch
    {
        UiTone.Neutral => string.Empty,
        UiTone.Accent => "ui-tone-accent",
        UiTone.Ok => "ui-tone-ok",
        UiTone.Warn => "ui-tone-warn",
        UiTone.Danger => "ui-tone-danger",
        _ => string.Empty,
    };

    /// <summary>Returns the badge class for a tone.</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>A CSS class name.</returns>
    public static string BadgeClass(UiTone tone) => tone switch
    {
        UiTone.Neutral => "ui-badge-neutral",
        UiTone.Accent => "ui-badge-accent",
        UiTone.Ok => "ui-badge-ok",
        UiTone.Warn => "ui-badge-warn",
        UiTone.Danger => "ui-badge-danger",
        _ => "ui-badge-neutral",
    };

    /// <summary>Returns a CSS colour expression for a tone (a custom property reference).</summary>
    /// <param name="tone">The tone.</param>
    /// <returns>A <c>var(--…)</c> expression.</returns>
    public static string ToneColor(UiTone tone) => tone switch
    {
        UiTone.Neutral => "var(--text-muted)",
        UiTone.Accent => "var(--accent)",
        UiTone.Ok => "var(--ok)",
        UiTone.Warn => "var(--warn)",
        UiTone.Danger => "var(--danger)",
        _ => "var(--text-muted)",
    };
}
