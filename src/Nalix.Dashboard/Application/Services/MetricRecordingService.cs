// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using Nalix.Dashboard.Application.Abstractions;
using Nalix.Dashboard.Application.Settings;
using Nalix.Dashboard.Application.State;
using Nalix.Dashboard.Domain.Reports;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Services;

internal sealed class MetricRecordingService : IDisposable
{
    private readonly IDashboardStateReader _stateReader;
    private readonly IAdminClient _client;
    private readonly IReportPollingController _pollingController;
    private readonly Lock _lock = new();

    private readonly Dictionary<RuntimeObservationTarget, RingBuffer<RecordedSnapshot>> _buffers = [];
    private readonly Dictionary<RuntimeObservationTarget, byte[]> _lastObservationBytes = [];
    private readonly HashSet<RuntimeObservationTarget> _selectedTargets = [];

    private bool _isRecording;
    private DateTimeOffset? _startTime;
    private CancellationTokenSource? _pollingCts;
    private Task? _backgroundTask;
    private int _capacityLimit = 10000;
    private int _intervalSeconds = 3;

    public event Action? Changed;

    public bool IsRecording
    {
        get { lock (_lock) { return _isRecording; } }
    }

    public DateTimeOffset? StartTime
    {
        get { lock (_lock) { return _startTime; } }
    }

    public IReadOnlySet<RuntimeObservationTarget> SelectedTargets
    {
        get { lock (_lock) { return new HashSet<RuntimeObservationTarget>(_selectedTargets); } }
    }

    public MetricRecordingService(
        IDashboardStateReader stateReader,
        IAdminClient client,
        IReportPollingController pollingController)
    {
        _stateReader = stateReader;
        _client = client;
        _pollingController = pollingController;

        _stateReader.ReportUpdated += OnReportUpdated;
    }

    public void Start(IEnumerable<RuntimeObservationTarget> targets, int intervalSeconds, int capacityLimit)
    {
        lock (_lock)
        {
            if (_isRecording)
            {
                return;
            }

            _selectedTargets.Clear();
            foreach (var target in targets)
            {
                if (target != RuntimeObservationTarget.NONE)
                {
                    _selectedTargets.Add(target);
                }
            }

            if (_selectedTargets.Count == 0)
            {
                return;
            }

            _capacityLimit = Math.Max(100, capacityLimit);
            _intervalSeconds = Math.Max(1, intervalSeconds);

            // Re-initialize buffers for selected targets
            _buffers.Clear();
            _lastObservationBytes.Clear();
            foreach (var target in _selectedTargets)
            {
                _buffers[target] = new RingBuffer<RecordedSnapshot>(_capacityLimit);
            }

            _isRecording = true;
            _startTime = DateTimeOffset.Now;

            // Start background polling loop for non-active targets
            _pollingCts = new CancellationTokenSource();
            _backgroundTask = RunBackgroundPollingAsync(_pollingCts.Token);
        }

        NotifyChanged();
    }

    public void Stop()
    {
        CancellationTokenSource? ctsToCancel = null;
        Task? taskToWait = null;

        lock (_lock)
        {
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;
            ctsToCancel = _pollingCts;
            _pollingCts = null;
            taskToWait = _backgroundTask;
            _backgroundTask = null;
        }

        if (ctsToCancel != null)
        {
            try
            {
                ctsToCancel.Cancel();
                ctsToCancel.Dispose();
            }
            catch { /* ignore */ }
        }

        if (taskToWait != null)
        {
            try
            {
                taskToWait.GetAwaiter().GetResult();
            }
            catch { /* ignore */ }
        }

        NotifyChanged();
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var buffer in _buffers.Values)
            {
                buffer.Clear();
            }
            _lastObservationBytes.Clear();
            if (!_isRecording)
            {
                _startTime = null;
            }
        }
        NotifyChanged();
    }

    public int GetRecordedCount(RuntimeObservationTarget target)
    {
        lock (_lock)
        {
            return _buffers.TryGetValue(target, out var buffer) ? buffer.Count : 0;
        }
    }

    public IReadOnlyList<RecordedSnapshot> GetSnapshots(RuntimeObservationTarget target)
    {
        lock (_lock)
        {
            return _buffers.TryGetValue(target, out var buffer) ? buffer.ToList() : Array.Empty<RecordedSnapshot>();
        }
    }

    private void OnReportUpdated(DashboardReportSnapshot report)
    {
        lock (_lock)
        {
            if (!_isRecording || !_selectedTargets.Contains(report.Target))
            {
                return;
            }

            // Deduplication logic
            byte[] currentBytes = report.ObservationData.ToArray();
            if (_lastObservationBytes.TryGetValue(report.Target, out byte[]? lastBytes))
            {
                if (currentBytes.AsSpan().SequenceEqual(lastBytes))
                {
                    // Skip recording if data is identical
                    return;
                }
            }

            _lastObservationBytes[report.Target] = currentBytes;

            if (_buffers.TryGetValue(report.Target, out var buffer))
            {
                buffer.Add(new RecordedSnapshot(report.Target, report.ReceivedAt, report.ObservationData));
            }
        }

        NotifyChanged();
    }

    private async Task RunBackgroundPollingAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            List<RuntimeObservationTarget> targetsToPoll = [];
            int intervalMs;

            lock (_lock)
            {
                if (!_isRecording) break;
                intervalMs = _intervalSeconds * 1000;

                foreach (var target in _selectedTargets)
                {
                    // Check if UI is actively polling this target
                    bool isPolledByUI = _pollingController.IsPolling && _pollingController.ActiveTarget == target;
                    if (!isPolledByUI)
                    {
                        targetsToPoll.Add(target);
                    }
                }
            }

            // Perform active background requests
            foreach (var target in targetsToPoll)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await _client.RefreshAsync(target, ct).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Ignore transient exceptions in background recording polling
                }
            }

            try
            {
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void NotifyChanged() => Changed?.Invoke();

    public void Dispose()
    {
        _stateReader.ReportUpdated -= OnReportUpdated;
        this.Stop();
    }
}

internal sealed record RecordedSnapshot(
    RuntimeObservationTarget Target,
    DateTimeOffset Timestamp,
    ReadOnlyMemory<byte> ObservationData);

internal sealed class RingBuffer<T>
{
    private readonly T[] _array;
    private int _head;
    private int _tail;
    private int _count;

    public RingBuffer(int capacity)
    {
        _array = new T[capacity];
    }

    public int Count => _count;
    public int Capacity => _array.Length;

    public void Add(T item)
    {
        _array[_tail] = item;
        _tail = (_tail + 1) % _array.Length;
        if (_count < _array.Length)
        {
            _count++;
        }
        else
        {
            _head = (_head + 1) % _array.Length;
        }
    }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
        _count = 0;
        Array.Clear(_array, 0, _array.Length);
    }

    public List<T> ToList()
    {
        List<T> list = new(_count);
        for (int i = 0; i < _count; i++)
        {
            list.Add(_array[(_head + i) % _array.Length]);
        }
        return list;
    }
}
