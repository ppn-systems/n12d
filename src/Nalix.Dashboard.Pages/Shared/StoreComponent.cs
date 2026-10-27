// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Components;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Dashboard.Controls;
using Nalix.Dashboard.State;
using Nalix.Dashboard.State.Metrics;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Pages.Shared;

/// <summary>
/// Base for views bound to <see cref="DashboardStore"/>: re-renders on store changes and keeps the
/// sections returned by <see cref="WatchedTargets"/> in the sampled set while the view is open.
/// </summary>
public abstract class StoreComponent : ComponentBase, IDisposable
{
    private IDisposable? _watch;
    private RuntimeObservationTargets _watched;
    private bool _disposed;

    /// <summary>Gets the dashboard store.</summary>
    [Inject]
    protected DashboardStore Store { get; set; } = default!;

    /// <summary>Gets the sections this view needs sampled; override to request more than the basics.</summary>
    protected virtual RuntimeObservationTargets WatchedTargets => RuntimeObservationTargets.NONE;

    /// <summary>Releases the store subscription.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected override void OnInitialized() => Store.Changed += ON_CHANGED;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        RuntimeObservationTargets targets = WatchedTargets;
        if (targets != _watched)
        {
            _watch?.Dispose();
            _watch = targets == RuntimeObservationTargets.NONE ? null : Store.Watch(targets);
            _watched = targets;
        }
    }

    /// <summary>Releases the store subscription.</summary>
    /// <param name="disposing">Whether called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
            Store.Changed -= ON_CHANGED;
            _watch?.Dispose();
        }
    }

    /// <summary>Gets a metric series by path.</summary>
    /// <param name="path">The metric path.</param>
    /// <returns>The series, or <see langword="null"/>.</returns>
    protected MetricSeries? Series(string path) => Store.History.Get(path);

    /// <summary>Gets the latest value of a metric, or NaN.</summary>
    /// <param name="path">The metric path.</param>
    /// <returns>The value.</returns>
    protected double Latest(string path) => Store.History.Get(path)?.Latest ?? double.NaN;

    /// <summary>Formats the latest value of a metric with <see cref="UiFormat.Auto"/> rules.</summary>
    /// <param name="path">The metric path.</param>
    /// <returns>The display text.</returns>
    protected string Show(string path)
    {
        int dot = path.LastIndexOf('.');
        return UiFormat.Auto(dot < 0 ? path : path[(dot + 1)..], Latest(path));
    }

    /// <summary>Builds the URL of the metric explorer for a path.</summary>
    /// <param name="path">The metric path.</param>
    /// <returns>The relative URL.</returns>
    protected static string MetricUrl(string path) => "metric?p=" + Uri.EscapeDataString(path);

    /// <summary>Gets the section that owns a metric path, from its first segment.</summary>
    /// <param name="path">The metric path.</param>
    /// <returns>The section, or <see langword="null"/> for derived or unknown paths.</returns>
    protected static SectionInfo? SectionOf(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        int end = path.IndexOfAny(['.', '[']);
        string key = end < 0 ? path : path[..end];
        foreach (SectionInfo section in Sections.All)
        {
            if (string.Equals(section.Key, key, StringComparison.Ordinal))
            {
                return section;
            }
        }

        return null;
    }

    private void ON_CHANGED() => _ = InvokeAsync(StateHasChanged);
}
