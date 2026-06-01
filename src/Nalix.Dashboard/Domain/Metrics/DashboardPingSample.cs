// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Metrics;

public sealed record DashboardPingSample(DateTimeOffset Timestamp, double Milliseconds);
