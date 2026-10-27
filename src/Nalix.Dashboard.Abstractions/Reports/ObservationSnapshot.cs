// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Abstractions.Reports;

/// <summary>
/// One parsed observation response.
/// </summary>
/// <param name="SampledAt">Server sampling time in Unix milliseconds.</param>
/// <param name="Targets">The sections present in <paramref name="Root"/>.</param>
/// <param name="Root">The payload object; each child is one section keyed by <see cref="SectionInfo.Key"/>.</param>
/// <param name="PayloadBytes">The size of the JSON payload as received.</param>
/// <param name="RoundTripMs">The request round-trip time in milliseconds.</param>
public sealed record ObservationSnapshot(long SampledAt, RuntimeObservationTargets Targets, ReportNode Root, int PayloadBytes, double RoundTripMs)
{
    /// <summary>
    /// Gets the node of a section, or <see langword="null"/> when the section is not in this snapshot.
    /// </summary>
    /// <param name="section">The section.</param>
    /// <returns>The section node.</returns>
    public ReportNode? Get(SectionInfo section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return Root.Get(section.Key);
    }
}
