// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.Connection;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Dashboard.Abstractions.Settings;
using Nalix.Dashboard.State.Logging;
using Nalix.Dashboard.State.Metrics;
using Nalix.Dashboard.State.Recording;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.State;

/// <summary>
/// The single source of truth for the dashboard: connection, sampling loop, metric history and recording.
/// </summary>
/// <remarks>
/// Each tick sends ONE request whose target mask is the union of what the open views watch, the overview
/// basics, and everything while recording. Views subscribe with <see cref="Watch"/> and re-render on
/// <see cref="Changed"/>. Blazor WebAssembly is single-threaded, so state is not locked.
/// </remarks>
public sealed class DashboardStore : IAsyncDisposable
{
    /// <summary>Sections sampled on every tick: the overview and the header read them.</summary>
    public const RuntimeObservationTargets BaseTargets =
        RuntimeObservationTargets.PROCESS | RuntimeObservationTargets.CONNECTIONS | RuntimeObservationTargets.DISPATCH;

    private const int FailuresBeforeReconnect = 3;
    private const int MaxReconnectDelayMs = 30_000;

    private readonly IObservabilityClient _client;
    private readonly ISettingsStore _settingsStore;
    private readonly Dictionary<string, ReportNode> _sections = new(StringComparer.Ordinal);
    private readonly int[] _watchers = new int[32];
    private CancellationTokenSource? _loopCts;
    private CancellationTokenSource? _reconnectCts;
    private string? _key;
    private int _consecutiveFailures;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardStore"/> class.
    /// </summary>
    /// <param name="client">The observability transport.</param>
    /// <param name="settingsStore">The browser settings store.</param>
    public DashboardStore(IObservabilityClient client, ISettingsStore settingsStore)
    {
        _client = client;
        _settingsStore = settingsStore;
        _client.Disconnected += ON_DISCONNECTED;
        History = new MetricHistory(Settings.HistorySize);
    }

    /// <summary>Raised after any state change; handlers must marshal to their render context.</summary>
    public event Action? Changed;

    /// <summary>Gets the connection status.</summary>
    public ConnectionStatus Status { get; private set; }

    /// <summary>Gets the last connection or sampling error.</summary>
    public string? Error { get; private set; }

    /// <summary>Gets the current preferences.</summary>
    public DashboardSettings Settings { get; private set; } = new();

    /// <summary>Gets a value indicating whether sampling is paused.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Gets a value indicating whether an automatic reconnect is in progress.</summary>
    public bool IsReconnecting => _reconnectCts is not null;

    /// <summary>Gets the per-metric history.</summary>
    public MetricHistory History { get; private set; }

    /// <summary>Gets the recorder.</summary>
    public MetricRecorder Recorder { get; } = new();

    /// <summary>Gets the dashboard's own event log.</summary>
    public EventLog Log { get; } = new();

    /// <summary>Gets the most recent snapshot.</summary>
    public ObservationSnapshot? Latest { get; private set; }

    /// <summary>Gets the number of successful samples since connecting.</summary>
    public long SampleCount { get; private set; }

    /// <summary>
    /// Gets the latest node of a section, even if the most recent tick did not sample it.
    /// </summary>
    /// <param name="section">The section.</param>
    /// <returns>The node, or <see langword="null"/> when never received.</returns>
    public ReportNode? GetSection(SectionInfo section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return _sections.GetValueOrDefault(section.Key);
    }

    /// <summary>
    /// Loads preferences and reconnects with a stored key, if any. Safe to call more than once.
    /// </summary>
    /// <returns>A task completing when initialized.</returns>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Settings = await _settingsStore.LoadAsync();
        History = new MetricHistory(Settings.HistorySize);

        string? key = await _settingsStore.LoadKeyAsync();
        if (!string.IsNullOrWhiteSpace(key))
        {
            await ConnectAsync(Settings.Endpoint, key, Settings.RememberKey);
        }
        else
        {
            NOTIFY();
        }
    }

    /// <summary>
    /// Connects to <paramref name="endpoint"/> and starts sampling.
    /// </summary>
    /// <param name="endpoint">The server endpoint.</param>
    /// <param name="key">The access key.</param>
    /// <param name="rememberKey">Whether to keep the key across browser restarts.</param>
    /// <returns><see langword="true"/> when connected.</returns>
    public async Task<bool> ConnectAsync(ServerEndpoint endpoint, string key, bool rememberKey)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        STOP_RECONNECT();
        STOP_LOOP();

        Settings = Settings with { Endpoint = endpoint, RememberKey = rememberKey };
        await _settingsStore.SaveAsync(Settings);

        if (await TRY_CONNECT(endpoint, key))
        {
            await _settingsStore.SaveKeyAsync(key, rememberKey);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Disconnects, stops sampling and forgets the in-memory key (a stored key is cleared too).
    /// </summary>
    /// <returns>A task completing when disconnected.</returns>
    public async Task DisconnectAsync()
    {
        STOP_RECONNECT();
        STOP_LOOP();
        _key = null;
        await _client.DisconnectAsync();
        await _settingsStore.SaveKeyAsync(null, remember: false);
        Status = ConnectionStatus.Disconnected;
        Error = null;
        Log.Add(LogLevel.Info, "Disconnected.");
        NOTIFY();
    }

    /// <summary>
    /// Adds <paramref name="targets"/> to the sampled set until the returned handle is disposed.
    /// </summary>
    /// <param name="targets">The sections a view needs.</param>
    /// <returns>The subscription handle.</returns>
    public IDisposable Watch(RuntimeObservationTargets targets)
    {
        ADJUST_WATCHERS(targets, +1);
        if (Status == ConnectionStatus.Connected && (targets & ~BaseTargets) != 0)
        {
            _ = SAMPLE_ONCE(CancellationToken.None);
        }

        return new Subscription(this, targets);
    }

    /// <summary>
    /// Pauses or resumes sampling.
    /// </summary>
    /// <param name="paused">Whether to pause.</param>
    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Log.Add(LogLevel.Info, paused ? "Sampling paused." : "Sampling resumed.");
        NOTIFY();
    }

    /// <summary>
    /// Applies and persists new preferences, restarting the loop when the interval or history size changes.
    /// </summary>
    /// <param name="settings">The new preferences.</param>
    /// <returns>A task completing when applied.</returns>
    public async Task UpdateSettingsAsync(DashboardSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        DashboardSettings previous = Settings;
        Settings = settings with
        {
            IntervalMs = Math.Clamp(settings.IntervalMs, 250, 60_000),
            HistorySize = Math.Clamp(settings.HistorySize, 60, 7200),
        };
        await _settingsStore.SaveAsync(Settings);

        if (Settings.HistorySize != previous.HistorySize)
        {
            History = new MetricHistory(Settings.HistorySize);
        }

        if (Settings.IntervalMs != previous.IntervalMs && Status == ConnectionStatus.Connected)
        {
            START_LOOP();
        }

        NOTIFY();
    }

    /// <summary>Starts a recording.</summary>
    public void StartRecording()
    {
        Recorder.Start();
        Log.Add(LogLevel.Info, "Recording started.");
        NOTIFY();
    }

    /// <summary>Stops the recording.</summary>
    public void StopRecording()
    {
        Recorder.Stop();
        Log.Add(LogLevel.Info, $"Recording stopped: {Recorder.RowCount} samples, {Recorder.ColumnCount} metrics.");
        NOTIFY();
    }

    /// <summary>Clears the metric history.</summary>
    public void ClearHistory()
    {
        History = new MetricHistory(Settings.HistorySize);
        NOTIFY();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        STOP_RECONNECT();
        STOP_LOOP();
        _client.Disconnected -= ON_DISCONNECTED;
        await _client.DisposeAsync();
    }

    private async Task<bool> TRY_CONNECT(ServerEndpoint endpoint, string key)
    {
        Error = null;
        Status = ConnectionStatus.Connecting;
        NOTIFY();

        try
        {
            await _client.ConnectAsync(endpoint, key, status =>
            {
                Status = status;
                NOTIFY();
            }, CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Status = ConnectionStatus.Faulted;
            Error = ex.Message;
            Log.Add(LogLevel.Error, $"Connect to {endpoint} failed: {ex.Message}");
            NOTIFY();
            return false;
        }

        _key = key;
        _consecutiveFailures = 0;
        SampleCount = 0;
        Status = ConnectionStatus.Connected;
        Log.Add(LogLevel.Info, $"Connected to {endpoint}.");
        START_LOOP();
        return true;
    }

    private void START_LOOP()
    {
        STOP_LOOP();
        _loopCts = new CancellationTokenSource();
        _ = RUN_LOOP(Settings.IntervalMs, _loopCts.Token);
    }

    private void STOP_LOOP()
    {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _loopCts = null;
    }

    private async Task RUN_LOOP(int intervalMs, CancellationToken ct)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(intervalMs));
        try
        {
            do
            {
                if (!IsPaused)
                {
                    await SAMPLE_ONCE(ct);
                }
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException)
        {
            // Loop stopped.
        }
    }

    private async Task SAMPLE_ONCE(CancellationToken ct)
    {
        RuntimeObservationTargets targets = Recorder.IsRecording ? RuntimeObservationTargets.ALL : BaseTargets | WATCHED();

        ObservationSnapshot snapshot;
        try
        {
            snapshot = await _client.SampleAsync(targets, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _consecutiveFailures++;
            Error = ex.Message;
            Log.Add(LogLevel.Warning, $"Sample failed: {ex.Message}");
            if (!_client.IsConnected || _consecutiveFailures >= FailuresBeforeReconnect)
            {
                BEGIN_RECONNECT(ex.Message);
            }

            NOTIFY();
            return;
        }

        _consecutiveFailures = 0;
        Error = null;
        Latest = snapshot;
        SampleCount++;
        foreach (ReportNode section in snapshot.Root.Children)
        {
            _sections[section.Name] = section;
        }

        History.Record(snapshot);
        Recorder.Capture(History, snapshot.SampledAt);
        NOTIFY();
    }

    private void ON_DISCONNECTED(string reason)
    {
        if (Status is ConnectionStatus.Connected)
        {
            BEGIN_RECONNECT(reason);
            NOTIFY();
        }
    }

    private void BEGIN_RECONNECT(string reason)
    {
        STOP_LOOP();
        Status = ConnectionStatus.Faulted;
        Error = reason;
        if (_key is null || _reconnectCts is not null)
        {
            return;
        }

        Log.Add(LogLevel.Warning, $"Connection lost: {reason}. Reconnecting…");
        _reconnectCts = new CancellationTokenSource();
        _ = RECONNECT_LOOP(_key, _reconnectCts.Token);
    }

    private async Task RECONNECT_LOOP(string key, CancellationToken ct)
    {
        int delayMs = 1000;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(delayMs, ct);
                if (await TRY_CONNECT(Settings.Endpoint, key))
                {
                    break;
                }

                delayMs = Math.Min(delayMs * 2, MaxReconnectDelayMs);
            }
        }
        catch (OperationCanceledException)
        {
            // Reconnect cancelled by the user.
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                _reconnectCts?.Dispose();
                _reconnectCts = null;
                NOTIFY();
            }
        }
    }

    private void STOP_RECONNECT()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = null;
    }

    private RuntimeObservationTargets WATCHED()
    {
        uint mask = 0;
        for (int bit = 0; bit < _watchers.Length; bit++)
        {
            if (_watchers[bit] > 0)
            {
                mask |= 1u << bit;
            }
        }

        return (RuntimeObservationTargets)mask;
    }

    private void ADJUST_WATCHERS(RuntimeObservationTargets targets, int delta)
    {
        uint mask = (uint)targets;
        for (int bit = 0; bit < _watchers.Length; bit++)
        {
            if ((mask & (1u << bit)) != 0)
            {
                _watchers[bit] = Math.Max(0, _watchers[bit] + delta);
            }
        }
    }

    private void NOTIFY() => Changed?.Invoke();

    private sealed class Subscription(DashboardStore owner, RuntimeObservationTargets targets) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                owner.ADJUST_WATCHERS(targets, -1);
            }
        }
    }
}
