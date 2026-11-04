// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Pages.Shared;

/// <summary>
/// The focus metrics shown above each section report, keyed by section key (<c>SectionInfo.Key</c>).
/// </summary>
/// <remarks>
/// Every path is a dot path into the section's report, prefixed with the section key, for example
/// <c>Dispatch.TotalPackets</c>. Sections without a meaningful scalar counter have no entry.
/// </remarks>
public static class SectionHighlights
{
    private static readonly IReadOnlyList<(string Label, string Path, int Slot)> s_none = [];

    private static readonly Dictionary<string, (string Label, string Path, int Slot)[]> s_highlights = new(StringComparer.Ordinal)
    {
        ["Process"] =
        [
            ("CPU", "Process.CpuPercent", 1),
            ("Working set", "Process.WorkingSetBytes", 2),
            ("Managed heap", "Process.ManagedHeapBytes", 3),
            ("Allocation rate", "Process.AllocationBytesPerSecond", 4),
            ("Thread pool threads", "Process.ThreadPoolThreads", 5),
            ("Lock contentions", "Process.LockContentions", 6),
        ],
        ["Dispatch"] =
        [
            ("Packets", "Dispatch.TotalPackets", 1),
            ("Ready connections", "Dispatch.ReadyConnections", 2),
            ("Running", "Dispatch.PipelineMetrics.ActiveExecutions", 3),
            ("Avg time (ms)", "Dispatch.PipelineMetrics.AverageTimeMs", 4),
            ("Errors", "Dispatch.PipelineMetrics.TotalErrors", 5),
            ("Dropped", "Dispatch.PipelineMetrics.TotalDropped", 6),
        ],
        ["Connections"] =
        [
            ("Connections", "Connections.TotalConnections", 1),
            ("Ingress", "Connections.IngressBytesPerSecond", 2),
            ("Egress", "Connections.EgressBytesPerSecond", 3),
            ("Packets dropped", "Connections.TotalPacketsDropped", 4),
            ("Pending callbacks", "Connections.AsyncCallback.PendingProcess", 5),
            ("Timers registered", "Connections.TimingWheel.RegisteredCount", 6),
        ],
        ["Tasks"] =
        [
            ("Workers running", "Tasks.WorkersRunning", 1),
            ("Workers", "Tasks.WorkersTotal", 2),
            ("Recurring jobs", "Tasks.RecurringCount", 3),
            ("Working set (MB)", "Tasks.Memory.WorkingSetMB", 4),
            ("Threads", "Tasks.Process.Threads", 5),
            ("Handles", "Tasks.Process.Handles", 6),
        ],
        ["Sessions"] =
        [
            ("Stores attempted", "Sessions.TotalStoresAttempted", 1),
            ("Stores succeeded", "Sessions.TotalStoresSucceeded", 2),
            ("Stores failed", "Sessions.TotalStoresFailed", 3),
            ("Stores rejected", "Sessions.TotalStoresRejectedByPolicy", 4),
            ("Consumes attempted", "Sessions.TotalConsumesAttempted", 5),
            ("Consumes failed", "Sessions.TotalConsumesFailed", 6),
        ],
        ["Buffers"] =
        [
            ("Rented", "Buffers.RentCount", 1),
            ("Returned", "Buffers.ReturnCount", 2),
            ("Outstanding", "Buffers.OutstandingCount", 3),
            ("Bytes rented", "Buffers.TotalBytesRented", 4),
            ("Throughput (MB/s)", "Buffers.ThroughputMBps", 5),
        ],
        ["ObjectPools"] =
        [
            ("Pools", "ObjectPools.PoolCount", 1),
            ("Gets", "ObjectPools.TotalGetOperations", 2),
            ("Cache hit rate", "ObjectPools.CacheHitRate", 3),
            ("Net objects", "ObjectPools.NetObjects", 4),
            ("Leaked", "ObjectPools.TotalLeaked", 5),
        ],
        ["Listeners"] =
        [
            ("TCP accepted", "Listeners.TCP.Metrics.TotalAccepted", 1),
            ("TCP rejected", "Listeners.TCP.Metrics.TotalRejected", 2),
            ("TCP errors", "Listeners.TCP.Metrics.TotalErrors", 3),
            ("UDP received", "Listeners.UDP.Traffic.ReceivedPackets", 4),
            ("UDP dropped", "Listeners.UDP.Traffic.TotalDropped", 5),
            ("UDP receive errors", "Listeners.UDP.Errors.ReceiveErrors", 6),
        ],
        ["Protocols"] =
        [
            ("TCP messages", "Protocols.TCP.TotalMessages", 1),
            ("TCP errors", "Protocols.TCP.TotalErrors", 2),
            ("UDP messages", "Protocols.UDP.TotalMessages", 3),
            ("UDP errors", "Protocols.UDP.TotalErrors", 4),
        ],
        ["ConnectionGuard"] =
        [
            ("Tracked endpoints", "ConnectionGuard.TrackedEndpoints", 1),
            ("Attempts", "ConnectionGuard.TotalAttempts", 2),
            ("Rejections", "ConnectionGuard.TotalRejections", 3),
            ("Rejection rate (%)", "ConnectionGuard.RejectionRate", 4),
            ("Global connections", "ConnectionGuard.GlobalConnections", 5),
        ],
        ["PolicyRateLimiter"] =
        [
            ("Checks", "PolicyRateLimiter.CheckCounter", 1),
            ("Hard blocked", "PolicyRateLimiter.SharedEngine.HardBlockedCount", 2),
            ("Soft warnings", "PolicyRateLimiter.SharedEngine.SoftWarningCount", 3),
            ("Tracked endpoints", "PolicyRateLimiter.SharedEngine.TrackedEndpoints", 4),
        ],
        ["TokenBucketLimiter"] =
        [
            ("Tracked endpoints", "TokenBucketLimiter.TrackedEndpoints", 1),
            ("Peak endpoints", "TokenBucketLimiter.PeakTrackedEndpoints", 2),
            ("Hard blocked", "TokenBucketLimiter.HardBlockedCount", 3),
            ("Soft warnings", "TokenBucketLimiter.SoftWarningCount", 4),
            ("Cleaned", "TokenBucketLimiter.TotalCleaned", 5),
        ],
        ["Instances"] =
        [
            ("Cached instances", "Instances.CachedInstanceCount", 1),
            ("Created", "Instances.InstanceCreationCount", 2),
            ("Cache hits", "Instances.InstanceCacheHitCount", 3),
            ("Disposables", "Instances.DisposableCount", 4),
        ],
    };

    /// <summary>
    /// Gets the highlight metrics of a section, in display order. Empty when the section has none.
    /// </summary>
    /// <param name="sectionKey">The section key, for example <c>Dispatch</c>.</param>
    /// <returns>The highlights; never <see langword="null"/>.</returns>
    public static IReadOnlyList<(string Label, string Path, int Slot)> For(string sectionKey)
    {
        return s_highlights.TryGetValue(sectionKey, out (string Label, string Path, int Slot)[]? items)
            ? items
            : s_none;
    }
}
