// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.Dispatch;

public sealed record DispatchReport(
    string? UtcNow,
    bool Running,
    long DispatchLoops,
    long TotalPackets,
    long PeakPackets,
    long TotalConnections,
    long ReadyConnections,
    long WakeSignals,
    long WakeReads,
    bool WakeRequested,
    string? PacketRegistryType,
    IReadOnlyDictionary<string, long>? PendingPerPriority,
    PipelineMetricsReport? PipelineMetrics,
    IReadOnlyList<MiddlewareMetricEntry>? MiddlewareMetrics);

public sealed record PipelineMetricsReport(
    long ActiveExecutions,
    long TotalExecutions,
    long TotalDropped,
    long TotalErrors,
    long DeserializationErrors,
    double AverageTimeMs,
    double MaxTimeMs);

public sealed record MiddlewareMetricEntry(
    string? Type,
    long TotalExecutions,
    long TotalErrors,
    double AverageTimeMs,
    double MaxTimeMs);
