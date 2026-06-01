// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Application.Settings;

namespace Nalix.Dashboard.Application.Abstractions;

internal interface IAdminSettingsStore
{
    Task<AdminSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(AdminSettings settings, CancellationToken ct = default);

    Task<string?> GetApiKeyAsync(CancellationToken ct = default);

    Task SetApiKeyAsync(string apiKey, AdminSettings settings, CancellationToken ct = default);

    Task ClearApiKeyAsync(CancellationToken ct = default);
}
