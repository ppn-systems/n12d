// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.ConnectionGuard;

public sealed record ConnectionGuardReport(
    string? UtcNow,
    int MaxPerEndpoint,
    double CleanupIntervalSeconds,
    double InactivityThresholdSeconds,
    int TrackedEndpoints,
    int PeakTrackedEndpoints,
    long TotalConcurrent,
    long TotalAttempts,
    long TotalRejections,
    long TotalCleaned,
    double RejectionRate,
    int SubnetTrackedV4,
    int SubnetTrackedV6,
    int GlobalConnections,
    int PeakGlobalConnections,
    int MaxGlobalConnections,
    int CurrentDifficulty,
    double EWMARate,
    bool AdaptiveActive);
