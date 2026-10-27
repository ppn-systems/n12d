// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Abstractions.Reports;

/// <summary>
/// Describes one report section: its wire flag, JSON key, route slug and display text.
/// </summary>
/// <param name="Target">The request flag that selects the section.</param>
/// <param name="Key">The top-level JSON property name (<see cref="RuntimeObservationSections"/>).</param>
/// <param name="Slug">The URL segment used by the section page.</param>
/// <param name="Title">The display title.</param>
/// <param name="Description">A one-line description shown under the title.</param>
public sealed record SectionInfo(RuntimeObservationTargets Target, string Key, string Slug, string Title, string Description);
