// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using Nalix.Abstractions.Networking.Protocols;
using Nalix.Abstractions.Primitives;
using Nalix.Abstractions.Security;
using Nalix.Dashboard.Abstractions.Connection;
using Nalix.Dashboard.Abstractions.History;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Observability.Contracts;
using Nalix.SDK.Options;
using Nalix.SDK.Transport;
using Nalix.SDK.Transport.Extensions;

namespace Nalix.Dashboard.Transport;

/// <summary>
/// <see cref="IObservabilityClient"/> over a Nalix SDK <see cref="WebSocketSession"/>.
/// </summary>
/// <remarks>
/// Requests are serialized through one gate: the dashboard samples on a timer, and an overlapping
/// sample would only add load on the server without making the view fresher.
/// </remarks>
public sealed class NalixObservabilityClient : IObservabilityClient
{
    private const int MaxMessageBytes = 8 * 1024 * 1024;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ReportJsonReader _reader = new();
    private WebSocketSession? _session;
    private int _timeoutMs = 5000;
    private bool _closing;

    /// <inheritdoc/>
    public event Action<string>? Disconnected;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public async Task ConnectAsync(ServerEndpoint endpoint, string accessKey, Action<ConnectionStatus>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        Bytes32 key = PARSE_KEY(accessKey);

        await _gate.WaitAsync(ct);
        try
        {
            await CLOSE_AsyncCore();

            _timeoutMs = endpoint.TimeoutMs;
            TransportOptions transport = new()
            {
                Address = endpoint.Host,
                Port = endpoint.Port,
                ConnectTimeoutMillis = endpoint.TimeoutMs,
                AutoReconnectEnabled = false,
                KeepAliveIntervalMillis = 0,
                ResumeEnabled = false,
            };
            WebSocketTransportOptions webSocket = new()
            {
                Path = endpoint.Path,
                UseTls = endpoint.UseTls,
                MaxMessageSize = MaxMessageBytes,
            };

            WebSocketSession session = new(transport, webSocket);
            session.OnDisconnected += ON_DISCONNECTED;
            _session = session;
            _closing = false;

            progress?.Invoke(ConnectionStatus.Connecting);
            await session.ConnectAsync(ct: ct);
            await session.HandshakeAsync(ct);

            progress?.Invoke(ConnectionStatus.Authorizing);
            using ObservabilityAccess request = ObservabilityAccess.Create();
            request.Initialize(ObservabilityAccessStage.REQUEST, accessKey: key);

            using ObservabilityAccess response = await session.RequestAsync<ObservabilityAccess>(
                request,
                RequestOptions.Default.WithTimeout(_timeoutMs).WithEncrypt(),
                predicate: static p => p.Stage == ObservabilityAccessStage.RESPONSE,
                ct);

            if (response.Reason != ProtocolReason.NONE || response.AccessLevel < PermissionLevel.SUPERVISOR)
            {
                throw new ObservabilityException(
                    response.Reason == ProtocolReason.UNAUTHORIZED ? "The access key was rejected." : $"Access denied ({response.Reason}).",
                    response.Reason);
            }

            IsConnected = true;
        }
        catch
        {
            await CLOSE_AsyncCore();
            throw;
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await CLOSE_AsyncCore();
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ObservationSnapshot> SampleAsync(RuntimeObservationTargets targets, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            WebSocketSession session = _session is not null && IsConnected
                ? _session
                : throw new ObservabilityException("Not connected.");

            using RuntimeObservation request = RuntimeObservation.Create();
            request.Initialize(RuntimeObservationStage.REQUEST, targets);

            long started = Stopwatch.GetTimestamp();
            using RuntimeObservation response = await session.RequestAsync<RuntimeObservation>(
                request,
                RequestOptions.Default.WithTimeout(_timeoutMs).WithEncrypt(),
                predicate: static p => p.Stage == RuntimeObservationStage.RESPONSE,
                ct);
            double roundTripMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (response.Reason is not ProtocolReason.NONE and not ProtocolReason.NOT_FOUND)
            {
                throw new ObservabilityException($"Sampling failed ({response.Reason}).", response.Reason);
            }

            ReadOnlySpan<byte> payload = response.ObservationData.Span;
            ReportNode root = _reader.Parse(payload);
            return new ObservationSnapshot(response.SampledAt, response.Targets, root, payload.Length, roundTripMs);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<HistoryCatalog> GetHistoryCatalogAsync(long from, long to, CancellationToken ct)
    {
        using JsonDocument json = await HISTORY_AsyncCore(from, to, 0, [], ct);
        JsonElement root = json.RootElement;

        List<HistoryDay> days = [];
        foreach (JsonElement day in root.GetProperty(RuntimeObservationHistory.Days).EnumerateArray())
        {
            days.Add(new HistoryDay(
                DateOnly.ParseExact(day.GetProperty("date").GetString()!, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                day.GetProperty(RuntimeObservationHistory.From).GetInt64(),
                day.GetProperty(RuntimeObservationHistory.To).GetInt64(),
                day.GetProperty("samples").GetInt32(),
                day.GetProperty("bytes").GetInt64()));
        }

        List<string> metrics = [];
        foreach (JsonElement metric in root.GetProperty(RuntimeObservationHistory.Metrics).EnumerateArray())
        {
            metrics.Add(metric.GetString()!);
        }

        return new HistoryCatalog(root.GetProperty(RuntimeObservationHistory.Interval).GetInt64(), days, metrics);
    }

    /// <inheritdoc/>
    public async Task<HistorySeries> GetHistoryAsync(long from, long to, int points, IReadOnlyList<string> metrics, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        if (metrics.Count == 0)
        {
            return new HistorySeries(from, to, 0, [], []);
        }

        using JsonDocument json = await HISTORY_AsyncCore(from, to, points, metrics, ct);
        JsonElement root = json.RootElement;

        JsonElement timesElement = root.GetProperty(RuntimeObservationHistory.Times);
        long[] times = new long[timesElement.GetArrayLength()];
        int index = 0;
        foreach (JsonElement time in timesElement.EnumerateArray())
        {
            times[index++] = time.GetInt64();
        }

        JsonElement series = root.GetProperty(RuntimeObservationHistory.Series);
        List<HistoryMetric> result = new(metrics.Count);
        foreach (string name in metrics)
        {
            if (series.TryGetProperty(name, out JsonElement metric))
            {
                result.Add(new HistoryMetric(name, VALUES(metric.GetProperty(RuntimeObservationHistory.Average)), VALUES(metric.GetProperty(RuntimeObservationHistory.Maximum))));
            }
        }

        return new HistorySeries(
            root.GetProperty(RuntimeObservationHistory.From).GetInt64(),
            root.GetProperty(RuntimeObservationHistory.To).GetInt64(),
            root.GetProperty(RuntimeObservationHistory.Step).GetInt64(),
            times,
            result);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
    }

    // Sends one HISTORY_REQUEST; an empty metric list asks for the catalog.
    private async Task<JsonDocument> HISTORY_AsyncCore(long from, long to, int points, IReadOnlyList<string> metrics, CancellationToken ct)
    {
        ArrayBufferWriter<byte> query = new(256);
        using (Utf8JsonWriter writer = new(query))
        {
            writer.WriteStartObject();
            writer.WriteNumber(RuntimeObservationHistory.From, from);
            writer.WriteNumber(RuntimeObservationHistory.To, to);
            if (metrics.Count > 0)
            {
                writer.WriteNumber(RuntimeObservationHistory.Points, points);
                writer.WriteStartArray(RuntimeObservationHistory.Metrics);
                foreach (string metric in metrics)
                {
                    writer.WriteStringValue(metric);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        await _gate.WaitAsync(ct);
        try
        {
            WebSocketSession session = _session is not null && IsConnected
                ? _session
                : throw new ObservabilityException("Not connected.");

            using RuntimeObservation request = RuntimeObservation.Create();
            request.Initialize(RuntimeObservationStage.HISTORY_REQUEST, RuntimeObservationTargets.NONE, observationData: query.WrittenMemory);

            using RuntimeObservation response = await session.RequestAsync<RuntimeObservation>(
                request,
                RequestOptions.Default.WithTimeout(Math.Max(_timeoutMs, 15_000)).WithEncrypt(),
                predicate: static p => p.Stage == RuntimeObservationStage.HISTORY_RESPONSE,
                ct);

            if (response.Reason != ProtocolReason.NONE)
            {
                throw new ObservabilityException(
                    response.Reason == ProtocolReason.OPERATION_UNSUPPORTED ? "History recording is disabled on this server." : $"History query failed ({response.Reason}).",
                    response.Reason);
            }

            // The packet returns its buffer to the pool on dispose, so the document must own a copy.
            return JsonDocument.Parse(response.ObservationData.ToArray());
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    private static double[] VALUES(JsonElement array)
    {
        double[] values = new double[array.GetArrayLength()];
        int index = 0;
        foreach (JsonElement value in array.EnumerateArray())
        {
            values[index++] = value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.NaN;
        }

        return values;
    }

    private void ON_DISCONNECTED(object? sender, Exception error)
    {
        if (_closing)
        {
            return;
        }

        IsConnected = false;
        Disconnected?.Invoke(error.Message);
    }

    private async Task CLOSE_AsyncCore()
    {
        IsConnected = false;
        WebSocketSession? session = _session;
        _session = null;
        if (session is null)
        {
            return;
        }

        _closing = true;
        session.OnDisconnected -= ON_DISCONNECTED;
        try
        {
            await session.DisconnectAsync();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Closing a broken socket can fail; the session is being discarded either way.
        }
        finally
        {
            session.Dispose();
        }
    }

    private static Bytes32 PARSE_KEY(string accessKey)
    {
        string trimmed = accessKey?.Trim() ?? string.Empty;
        if (trimmed.Length != Bytes32.Size * 2)
        {
            throw new ObservabilityException("The access key must be 64 hexadecimal characters.");
        }

        Span<byte> raw = stackalloc byte[Bytes32.Size];
        if (Convert.FromHexString(trimmed, raw, out _, out int written) != System.Buffers.OperationStatus.Done || written != Bytes32.Size)
        {
            throw new ObservabilityException("The access key must be 64 hexadecimal characters.");
        }

        return new Bytes32(raw);
    }
}
