// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.ConcurrencyGate;

public sealed record ConcurrencyGateReport(
    string? UtcNow,
    double CleanupIntervalMinutes,
    double MinIdleAgeMinutes,
    int TrackedOpcodes,
    long TotalAcquired,
    long TotalRejected,
    long TotalQueued,
    long TotalCleaned,
    double RejectionRate,
    CircuitBreakerInfo? CircuitBreaker,
    IReadOnlyList<ConcurrencyGateOpcode>? Opcodes);

public sealed record CircuitBreakerInfo(
    bool IsOpen,
    long Trips);

public sealed record ConcurrencyGateOpcode(
    string? Opcode,
    int Capacity,
    int InUse,
    int Available,
    bool Queuing,
    int QueueCount,
    string? QueueMax,
    bool IsIdle,
    string? LastUsedUtc);
