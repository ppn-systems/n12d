// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Dashboard.Transport;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Tests.TestSupport;

/// <summary>
/// Report and observation builders shared by the transport and state tests.
/// </summary>
internal static class ReportFixtures
{
    /// <summary>
    /// Parses a JSON string into a report tree with a fresh <see cref="ReportJsonReader"/>.
    /// </summary>
    public static ReportNode Parse(string json)
    {
        return new ReportJsonReader().Parse(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Builds an observation snapshot from a JSON payload, with a fixed 12.5 ms round trip.
    /// </summary>
    public static ObservationSnapshot Snapshot(long sampledAt, string json)
    {
        return new(sampledAt, RuntimeObservationTargets.ALL, Parse(json), json.Length, 12.5);
    }
}
