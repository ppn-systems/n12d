// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Abstractions;

internal interface IAdminClient
{
    Task SetApiKeyAsync(string apiKey);

    Task RefreshAsync(RuntimeObservationTarget target, CancellationToken ct);

    Task RefreshAllAsync(CancellationToken ct);

    Task PingAsync(CancellationToken ct);

    Task ForceDisconnectAsync();
}
