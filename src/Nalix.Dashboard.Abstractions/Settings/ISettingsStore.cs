// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Settings;

/// <summary>
/// Persists preferences and the access key in the browser.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Loads settings, returning defaults when none are stored.</summary>
    /// <returns>The settings.</returns>
    ValueTask<DashboardSettings> LoadAsync();

    /// <summary>Saves settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <returns>A task completing when saved.</returns>
    ValueTask SaveAsync(DashboardSettings settings);

    /// <summary>Loads the access key from session or local storage.</summary>
    /// <returns>The key, or <see langword="null"/>.</returns>
    ValueTask<string?> LoadKeyAsync();

    /// <summary>
    /// Saves the access key: to local storage when <paramref name="remember"/> is set, otherwise to
    /// session storage only. Passing <see langword="null"/> clears both.
    /// </summary>
    /// <param name="key">The key, or <see langword="null"/> to clear.</param>
    /// <param name="remember">Whether to persist across browser restarts.</param>
    /// <returns>A task completing when saved.</returns>
    ValueTask SaveKeyAsync(string? key, bool remember);
}
