// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.Connections;

public sealed record ConnectionsReport(
    string? UtcNow,
    long TotalConnections,
    int ShardCount,
    long TotalBytesSent,
    long TotalBytesReceived,
    long IngressBytesPerSecond,
    long EgressBytesPerSecond,
    long TotalPacketsDropped,
    AsyncCallbackInfo? AsyncCallback,
    TimingWheelInfo? TimingWheel);

public sealed record TimingWheelInfo(
    int RegisteredCount,
    int PeakRegistered,
    long TotalRegistrations,
    long TotalUnregistrations,
    long TotalTimeouts,
    long TotalStaleSkips,
    long MaxTickDrift);

public sealed record AsyncCallbackInfo(
    long PendingProcess,
    long PendingPost,
    long TotalInvoked,
    long Dropped,
    long SlowCallbacks,
    long FairnessCollisions,
    long FairnessEvictions);
