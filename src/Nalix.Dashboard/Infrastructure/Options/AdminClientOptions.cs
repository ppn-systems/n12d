// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Infrastructure.Options;

internal sealed class AdminClientOptions
{
    public string BackendHost { get; set; } = "localhost";

    public ushort BackendPort { get; set; } = 57206;
}
