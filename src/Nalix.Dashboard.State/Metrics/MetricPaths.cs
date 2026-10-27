// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.State.Metrics;

/// <summary>
/// Paths of metrics the dashboard derives itself rather than reading from a report.
/// </summary>
public static class MetricPaths
{
    /// <summary>Process CPU usage across all cores, 0–100.</summary>
    public const string CpuPercent = "Process.CpuPercent";

    /// <summary>Managed allocation rate.</summary>
    public const string AllocationBytesPerSecond = "Process.AllocationBytesPerSecond";

    /// <summary>Garbage collections (all generations) per second.</summary>
    public const string GcPerSecond = "Process.GcPerSecond";

    /// <summary>Request round-trip time of the observation request.</summary>
    public const string RoundTripMs = "Dashboard.RoundTripMs";

    /// <summary>Size of the observation payload.</summary>
    public const string PayloadBytes = "Dashboard.PayloadBytes";
}
