// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Reports;

namespace Nalix.Dashboard.Pages.Layout;

/// <summary>One page reachable from a module's tab row.</summary>
/// <param name="Label">The tab label and page title.</param>
/// <param name="Href">The relative URL.</param>
/// <param name="Icon">The <c>UiIcon</c> name.</param>
public sealed record NavPage(string Label, string Href, string Icon);

/// <summary>One rail entry; its pages become the tab row.</summary>
/// <param name="Id">A stable identifier.</param>
/// <param name="Label">The rail label.</param>
/// <param name="Icon">The <c>UiIcon</c> name.</param>
/// <param name="Pages">The module's pages, first one is the landing page.</param>
public sealed record NavModule(string Id, string Label, string Icon, IReadOnlyList<NavPage> Pages);

/// <summary>
/// The navigation map, after AtmosOS: the rail lists a handful of modules so it never scrolls, and the
/// selected module's pages are the tab row under the topbar.
/// </summary>
public static class NavModel
{
    /// <summary>Gets the settings destination pinned to the bottom of the rail.</summary>
    public static NavModule Settings { get; } = new("settings", "Settings", "settings", [new("Settings", "settings", "settings")]);

    /// <summary>Gets the rail modules in display order.</summary>
    public static IReadOnlyList<NavModule> Modules { get; } =
    [
        new("overview", "Overview", "overview", [new("Overview", "", "overview")]),
        new("runtime", "Runtime", "process", PAGES(Sections.Process, Sections.Tasks, Sections.Buffers, Sections.ObjectPools, Sections.Instances)),
        new("network", "Network", "connections", PAGES(Sections.Connections, Sections.Sessions, Sections.Dispatch, Sections.Listeners, Sections.Protocols)),
        new("protection", "Protection", "connection-guard", PAGES(Sections.ConnectionGuard, Sections.ConcurrencyGate, Sections.PolicyRateLimiter, Sections.TokenBucketLimiter)),
        new("analysis", "Analysis", "diagnostics",
        [
            new("History", "history", "clock"),
            new("Diagnostics", "diagnostics", "diagnostics"),
            new("Recordings", "recordings", "recordings"),
            new("Event log", "log", "log"),
        ]),
    ];

    /// <summary>
    /// Finds the module and page for a relative URL (no leading slash, query allowed).
    /// </summary>
    /// <param name="relative">The URL relative to the app base.</param>
    /// <returns>The module and, when it is one of the module's tabs, the page.</returns>
    public static (NavModule Module, NavPage? Page) Resolve(string relative)
    {
        string path = relative;
        int query = path.IndexOf('?', StringComparison.Ordinal);
        if (query >= 0)
        {
            path = path[..query];
        }

        path = path.Trim('/');

        // The metric explorer belongs to the module of the section its metric comes from.
        if (path == "metric" && query >= 0)
        {
            string? section = SECTION_OF_QUERY(relative[(query + 1)..]);
            if (section is not null)
            {
                path = section;
            }
        }

        foreach (NavModule module in Modules)
        {
            foreach (NavPage page in module.Pages)
            {
                if (string.Equals(page.Href, path, StringComparison.OrdinalIgnoreCase))
                {
                    return (module, page);
                }
            }
        }

        if (string.Equals(path, Settings.Pages[0].Href, StringComparison.OrdinalIgnoreCase))
        {
            return (Settings, Settings.Pages[0]);
        }

        return (Modules[0], null);
    }

    private static NavPage[] PAGES(params SectionInfo[] sections)
    {
        NavPage[] pages = new NavPage[sections.Length];
        for (int i = 0; i < sections.Length; i++)
        {
            pages[i] = new NavPage(sections[i].Title, "section/" + sections[i].Slug, sections[i].Slug);
        }

        return pages;
    }

    // "p=Process.WorkingSetBytes" → "section/process"
    private static string? SECTION_OF_QUERY(string query)
    {
        foreach (string pair in query.Split('&'))
        {
            if (!pair.StartsWith("p=", StringComparison.Ordinal))
            {
                continue;
            }

            string metric = Uri.UnescapeDataString(pair[2..]);
            int end = metric.IndexOfAny(['.', '[']);
            string key = end < 0 ? metric : metric[..end];
            foreach (SectionInfo section in Sections.All)
            {
                if (string.Equals(section.Key, key, StringComparison.Ordinal))
                {
                    return "section/" + section.Slug;
                }
            }
        }

        return null;
    }
}
