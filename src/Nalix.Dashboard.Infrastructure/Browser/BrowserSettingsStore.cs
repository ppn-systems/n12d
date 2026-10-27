// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Microsoft.JSInterop;
using Nalix.Dashboard.Abstractions.Settings;

namespace Nalix.Dashboard.Infrastructure.Browser;

/// <summary>
/// <see cref="ISettingsStore"/> over the browser's local and session storage (<c>wwwroot/js/n12d.js</c>).
/// </summary>
/// <remarks>
/// The access key goes to session storage by default, so closing the browser forgets it; it is written
/// to local storage only when the user opts in.
/// </remarks>
public sealed class BrowserSettingsStore(IJSRuntime js) : ISettingsStore
{
    private const string SettingsKey = "n12d.settings";
    private const string AccessKey = "n12d.key";

    /// <inheritdoc/>
    public async ValueTask<DashboardSettings> LoadAsync()
    {
        string? json = await js.InvokeAsync<string?>("n12d.storage.get", "local", SettingsKey);
        if (string.IsNullOrEmpty(json))
        {
            return new DashboardSettings();
        }

        try
        {
            return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.DashboardSettings) ?? new DashboardSettings();
        }
        catch (JsonException)
        {
            return new DashboardSettings();
        }
    }

    /// <inheritdoc/>
    public ValueTask SaveAsync(DashboardSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.DashboardSettings);
        return js.InvokeVoidAsync("n12d.storage.set", "local", SettingsKey, json);
    }

    /// <inheritdoc/>
    public async ValueTask<string?> LoadKeyAsync()
        => await js.InvokeAsync<string?>("n12d.storage.get", "session", AccessKey)
           ?? await js.InvokeAsync<string?>("n12d.storage.get", "local", AccessKey);

    /// <inheritdoc/>
    public async ValueTask SaveKeyAsync(string? key, bool remember)
    {
        await js.InvokeVoidAsync("n12d.storage.remove", "local", AccessKey);
        await js.InvokeVoidAsync("n12d.storage.remove", "session", AccessKey);
        if (key is null)
        {
            return;
        }

        await js.InvokeVoidAsync("n12d.storage.set", remember ? "local" : "session", AccessKey, key);
    }
}
