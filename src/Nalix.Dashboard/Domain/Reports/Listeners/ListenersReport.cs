// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Domain.Reports.Listeners;

public sealed record ListenersReport(
    ListenerInfo? TCP,
    ListenerInfo? UDP,
    ListenerInfo? WEBSOCKET);

public sealed record ListenerInfo(
    string? UtcNow,
    int Port,
    string? State,
    bool Disposed,
    bool? IsDisposed,
    bool? IsListening,
    string? ProtocolType,
    ListenerConfiguration? Configuration,
    UdpConfiguration? Config,
    ListenerMetrics? Metrics,
    UdpTraffic? Traffic,
    UdpErrors? Errors,
    ListenerProtocol? Protocol,
    ListenerConnections? Connections,
    ListenerThreading? Threading,
    UdpWorker? Worker);

public sealed record ListenerConfiguration(
    bool EnableTimeout,
    int MaxParallelAccepts,
    int BufferSize,
    bool KeepAlive,
    bool ReuseAddress,
    bool EnableIPv6,
    int Backlog);

public sealed record ListenerMetrics(
    long TotalAccepted,
    long TotalRejected,
    long TotalErrors,
    long QueueFullRejections,
    long LimiterRejections,
    long ProxyProtocolErrors,
    long PendingProxyConnections,
    long AcceptQueueDepth);

public sealed record ListenerProtocol(
    string? BoundProtocol);

public sealed record ListenerConnections(
    int ActiveConnections,
    bool LimiterEnabled);

public sealed record ListenerThreading(
    int ThreadPoolMinWorker,
    int ThreadPoolMinIOCP);

public sealed record UdpConfiguration(
    bool ReuseAddress,
    int BufferSize,
    bool EnableIPv6,
    bool DualMode);

public sealed record UdpTraffic(
    long ReceivedPackets,
    long ReceivedBytes,
    long TotalDropped,
    long DroppedShort,
    long DroppedUnauth,
    long DroppedUnknown,
    long DroppedRateLimited,
    long DroppedOversize);

public sealed record UdpErrors(
    long ReceiveErrors);

public sealed record UdpWorker(
    string? Group,
    int GroupConcurrencyLimit);
