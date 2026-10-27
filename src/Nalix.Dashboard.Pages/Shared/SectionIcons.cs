// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Reports;

namespace Nalix.Dashboard.Pages.Shared;

/// <summary>
/// Maps sections to <c>UiIcon</c> names; the icon set uses the section slugs as names.
/// </summary>
public static class SectionIcons
{
    /// <summary>Gets the icon name of a section.</summary>
    /// <param name="section">The section.</param>
    /// <returns>The icon name.</returns>
    public static string Of(SectionInfo section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return section.Slug;
    }
}
