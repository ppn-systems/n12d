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
    long EgressBytesPerSecond);
