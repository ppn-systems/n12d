// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Controls.Icons;

/// <summary>
/// Maps the icon names n12d already uses onto AtmosOS icons. A name resolves to one of three things, in this order
/// (see <see cref="UiIconAliases.TryResolveBody(string, out string)"/> and <see cref="UiIconAliases.TryResolveAsset(string, out string)"/>):
/// a <see cref="NavIcons"/> body, a monochrome SVG from the shared icon set, or a drawn body kept here because AtmosOS has no
/// equivalent. Drawn bodies are lucide-style, 24x24, stroke-based, and are marked as drawn.
/// </summary>
internal static class UiIconAliases
{
    /// <summary>Names that point at a <see cref="NavIcons"/> body (AtmosOS nav icon, kebab-case key).</summary>
    private static readonly Dictionary<string, string> s_navKeys = new(StringComparer.Ordinal)
    {
        ["overview"] = "dashboard",
        ["process"] = "group-platform-system",
        ["sessions"] = "staff",
        ["buffers"] = "group-platform",
        ["connection-guard"] = "shield-check",
        ["concurrency-gate"] = "occupancy",
        ["policy-rate-limiter"] = "gauge",
        ["recordings"] = "camera",
        ["log"] = "audit-log",
    };

    /// <summary>Names drawn for n12d because AtmosOS has no matching glyph. Lucide-style; the wrapper supplies stroke-width 2.</summary>
    private static readonly Dictionary<string, string> s_drawn = new(StringComparer.Ordinal)
    {
        // Drawn: dispatch (lucide "send", a paper plane leaving a node).
        ["dispatch"] = """<path d='M22 2 11 13'/><path d='M22 2 15 22l-4-9-9-4 20-7z'/>""",
        // Drawn: connections (lucide "network").
        ["connections"] = """<rect x='16' y='16' width='6' height='6' rx='1'/><rect x='2' y='16' width='6' height='6' rx='1'/><rect x='9' y='2' width='6' height='6' rx='1'/><path d='M5 16v-3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v3'/><path d='M12 12V8'/>""",
        // Drawn: object pools (lucide "layers").
        ["object-pools"] = """<path d='M12 2 2 7l10 5 10-5-10-5z'/><path d='m2 17 10 5 10-5'/><path d='m2 12 10 5 10-5'/>""",
        // Drawn: listeners (lucide "radio").
        ["listeners"] = """<circle cx='12' cy='12' r='2'/><path d='M16.24 7.76a6 6 0 0 1 0 8.49m-8.48-.01a6 6 0 0 1 0-8.49m11.31-2.82a10 10 0 0 1 0 14.14m-14.14 0a10 10 0 0 1 0-14.14'/>""",
        // Drawn: protocols (code brackets).
        ["protocols"] = """<path d='M7 8l-4 4 4 4M17 8l4 4-4 4M14 4l-4 16'/>""",
        // Drawn: token bucket (a bucket with a handle).
        ["token-bucket-limiter"] = """<path d='M5 8h14l-1.5 12h-11L5 8z'/><path d='M9 8a3 3 0 0 1 6 0'/>""",
        // Drawn: instances (two stacked server bays).
        ["instances"] = """<rect x='4' y='3' width='16' height='7' rx='1.5'/><rect x='4' y='14' width='16' height='7' rx='1.5'/><path d='M8 6.5h.01M8 17.5h.01'/>""",
        // Drawn: diagnostics (a pulse trace).
        ["diagnostics"] = """<path d='M3 12h4l2-5 4 10 2-5h6'/>""",
        // Drawn: media transport.
        ["pause"] = """<path d='M8 5v14M16 5v14'/>""",
        ["stop"] = """<rect x='6' y='6' width='12' height='12' rx='1.5'/>""",
        ["record"] = """<circle cx='12' cy='12' r='7'/><circle cx='12' cy='12' r='2.5'/>""",
        ["download"] = """<path d='M12 4v11M7 10l5 5 5-5M5 20h14'/>""",
        // Drawn: plug with a slash (AtmosOS has the plain plug, not the struck-through form).
        ["unplug"] = """<path d='M9 3v5M15 3v5M7 8h10v3a5 5 0 0 1-10 0V8z'/><path d='M3 3l18 18'/>""",
        // Drawn: theme toggle.
        ["sun"] = """<circle cx='12' cy='12' r='4'/><path d='M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4'/>""",
        ["moon"] = """<path d='M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z'/>""",
        // Drawn: chevrons and tick (AtmosOS has the down chevron as a shared icon, but no left/right one).
        ["chevron-right"] = """<path d='M9 6l6 6-6 6'/>""",
        ["chevron-left"] = """<path d='M15 6l-6 6 6 6'/>""",
        ["check"] = """<path d='M5 12.5l4.5 4.5L19 7.5'/>""",
        // Drawn: refresh (circular arrow).
        ["refresh"] = """<path d='M20 11a8 8 0 1 0-2.3 5.7'/><path d='M20 4v7h-7'/>""",
    };

    /// <summary>Names that point at a monochrome SVG in <c>wwwroot/icons/shared</c>, served as a CSS mask.</summary>
    private static readonly Dictionary<string, string> s_assets = new(StringComparer.Ordinal)
    {
        ["play"] = "shared/player-play",
        ["plug"] = "shared/plug-connected",
        ["menu"] = "shared/menu",
        ["chevron-down"] = "shared/chevron-down",
        ["alert"] = "shared/alert-triangle",
        ["close"] = "shared/x",
    };

    /// <summary>
    /// Resolves an n12d icon name to an inline SVG body, through the nav alias table and then the drawn set.
    /// </summary>
    /// <param name="name">The n12d icon name, for example <c>overview</c>.</param>
    /// <param name="body">The SVG body when resolved; an empty string otherwise.</param>
    /// <returns><see langword="true"/> when the name maps to an inline body; otherwise <see langword="false"/>.</returns>
    public static bool TryResolveBody(string name, out string body)
    {
        if (s_navKeys.TryGetValue(name, out string? navKey) && NavIcons.TryGetBody(navKey, out body))
        {
            return true;
        }

        if (s_drawn.TryGetValue(name, out string? drawn))
        {
            body = drawn;
            return true;
        }

        body = string.Empty;
        return false;
    }

    /// <summary>
    /// Resolves an n12d icon name to a shared asset path (relative to the icons folder, without extension).
    /// </summary>
    /// <param name="name">The n12d icon name, for example <c>close</c>.</param>
    /// <param name="assetPath">The path such as <c>shared/x</c> when resolved; an empty string otherwise.</param>
    /// <returns><see langword="true"/> when the name maps to a shared asset; otherwise <see langword="false"/>.</returns>
    public static bool TryResolveAsset(string name, out string assetPath)
    {
        if (s_assets.TryGetValue(name, out string? path))
        {
            assetPath = path;
            return true;
        }

        assetPath = string.Empty;
        return false;
    }
}
