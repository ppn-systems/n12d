// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.ConnectionGuard;

public sealed record ConnectionGuardReport(
    string? UtcNow,
    int MaxPerEndpoint,
    double CleanupIntervalSeconds,
    double InactivityThresholdSeconds,
    int TrackedEndpoints,
    long TotalConcurrent,
    long TotalAttempts,
    long TotalRejections,
    long TotalCleaned,
    double RejectionRate,
    int SubnetTrackedV4,
    int SubnetTrackedV6,
    double EWMARate,
    bool AdaptiveActive);
