// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Domain.Reports.TokenBucketLimiter;

namespace Nalix.Dashboard.Domain.Reports.PolicyRateLimiter;

public sealed record PolicyRateLimiterReport(
    string? UtcNow,
    int CheckCounter,
    TokenBucketLimiterReport? SharedEngine);
