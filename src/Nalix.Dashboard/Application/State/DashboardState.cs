using Nalix.Dashboard.Domain.Logs;
using Nalix.Dashboard.Domain.Metrics;
using Nalix.Dashboard.Domain.Reports;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.State;

internal sealed class DashboardState : IDashboardStateReader, IDashboardStateWriter
{
    private readonly Lock _gate = new();
    private readonly Dictionary<RuntimeObservationTarget, DashboardReportSnapshot> _reports = [];
    private readonly Queue<DashboardLogEntry> _logs = [];
    private readonly Queue<DashboardPingSample> _pingSamples = [];
    private int _maxLogEntries = 250;
    private const int MaxPingSamples = 120;

    public event Action? Changed;

    public bool IsConnected { get; private set; }

    public bool HasApiKey { get; private set; }

    public string? LastError { get; private set; }

    public double? LastPingMilliseconds { get; private set; }

    public DateTimeOffset? LastPingAt { get; private set; }

    public IReadOnlyDictionary<RuntimeObservationTarget, DashboardReportSnapshot> Reports
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<RuntimeObservationTarget, DashboardReportSnapshot>(_reports);
            }
        }
    }

    public IReadOnlyList<DashboardPingSample> PingSamples
    {
        get
        {
            lock (_gate) { return [.. _pingSamples]; }
        }
    }

    public IReadOnlyList<DashboardLogEntry> Logs
    {
        get
        {
            lock (_gate) { return [.. _logs]; }
        }
    }

    public void SetApiKeyConfigured(bool configured)
    {
        lock (_gate)
        {
            this.HasApiKey = configured;
            if (!configured)
            {
                this.IsConnected = false;
                this.LastPingMilliseconds = null;
                this.LastPingAt = null;
            }
        }

        this.NotifyChanged();
    }

    public void SetMaxLogEntries(int max)
    {
        lock (_gate) { _maxLogEntries = Math.Max(50, max); }
    }

    public void Log(string level, string message)
    {
        lock (_gate) { this.EnqueueLogUnsafe(level, message); }
        this.NotifyChanged();
    }

    public void ClearLogs()
    {
        lock (_gate) { _logs.Clear(); }
        this.NotifyChanged();
    }

    public void MarkConnected()
    {
        lock (_gate)
        {
            this.IsConnected = true;
            this.LastError = null;
        }

        this.NotifyChanged();
    }

    public void MarkDisconnected(string? error)
    {
        lock (_gate)
        {
            this.IsConnected = false;
            this.LastError = error;
            this.LastPingMilliseconds = null;
            this.LastPingAt = null;
        }

        this.NotifyChanged();
    }

    public void UpdatePing(double milliseconds)
    {
        lock (_gate)
        {
            this.IsConnected = true;
            this.LastError = null;
            this.LastPingMilliseconds = milliseconds;
            this.LastPingAt = DateTimeOffset.Now;
            _pingSamples.Enqueue(new DashboardPingSample(this.LastPingAt.Value, milliseconds));
            while (_pingSamples.Count > MaxPingSamples)
            {
                _ = _pingSamples.Dequeue();
            }
        }

        this.NotifyChanged();
    }

    public void UpdateReport(DashboardReportSnapshot report)
    {
        lock (_gate)
        {
            _reports[report.Target] = report;
            this.IsConnected = true;
            this.LastError = null;
        }

        this.NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();

    private void EnqueueLogUnsafe(string level, string message)
    {
        _logs.Enqueue(new DashboardLogEntry(
            DateTimeOffset.Now,
            NormalizeLevel(level),
            NormalizeMessage(message)));

        while (_logs.Count > _maxLogEntries)
        {
            _ = _logs.Dequeue();
        }
    }

    private static string NormalizeLevel(string? level)
    {
        string normalized = string.IsNullOrWhiteSpace(level) ? "INFO" : level.Trim().ToUpperInvariant();
        return normalized switch
        {
            "TRACE" or "DEBUG" or "INFO" or "WARN" or "ERROR" or "CRITICAL" => normalized,
            "INFORMATION" => "INFO",
            "WARNING" => "WARN",
            _ => "INFO"
        };
    }

    private static string NormalizeMessage(string? message)
        => string.IsNullOrWhiteSpace(message) ? "(empty log message)" : message.Trim();
}
