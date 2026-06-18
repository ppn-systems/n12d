// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.TokenBucketLimiter;

public sealed record TokenBucketLimiterReport(
    string? UtcNow,
    double CapacityTokens,
    double RefillPerSecond,
    double TokenScale,
    int Shards,
    int HardLockoutSeconds,
    int StaleEntrySeconds,
    int CleanupIntervalSecs,
    int MaxTrackedEndpoints,
    int TrackedEndpoints,
    int PeakTrackedEndpoints,
    int HardBlockedCount,
    int SoftWarningCount,
    long TotalCleaned);
