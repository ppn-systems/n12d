// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Logs;

public sealed record DashboardLogEntry(DateTimeOffset Timestamp, string Level, string Message);
