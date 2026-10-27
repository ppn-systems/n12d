// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using Nalix.Abstractions.Networking.Protocols;
using Nalix.Abstractions.Primitives;
using Nalix.Abstractions.Security;
using Nalix.Dashboard.Abstractions.Connection;
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
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
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
