// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Connection;

namespace Nalix.Dashboard.Abstractions.Settings;

/// <summary>
/// User preferences persisted in the browser.
/// </summary>
public sealed record DashboardSettings
{
    /// <summary>Gets the last used server endpoint.</summary>
    public ServerEndpoint Endpoint { get; init; } = new();

    /// <summary>Gets the sampling interval in milliseconds.</summary>
    public int IntervalMs { get; init; } = 1000;

    /// <summary>Gets the number of samples kept per metric series.</summary>
    public int HistorySize { get; init; } = 600;

    /// <summary>Gets a value indicating whether the access key is kept in local storage across browser restarts.</summary>
    public bool RememberKey { get; init; }

    /// <summary>Gets the colour theme: <c>system</c>, <c>light</c> or <c>dark</c>.</summary>
    public string Theme { get; init; } = "system";
}
