// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Connection;

/// <summary>
/// Where and how to reach a Nalix server's WebSocket listener.
/// </summary>
public sealed record ServerEndpoint
{
    /// <summary>Gets the host name or address.</summary>
    public string Host { get; init; } = "localhost";

    /// <summary>Gets the TCP port.</summary>
    public ushort Port { get; init; } = 57207;

    /// <summary>Gets the WebSocket path.</summary>
    public string Path { get; init; } = "/ws/";

    /// <summary>Gets a value indicating whether to use <c>wss://</c>.</summary>
    public bool UseTls { get; init; }

    /// <summary>Gets the connect and request timeout in milliseconds.</summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <inheritdoc/>
    public override string ToString() => $"{(UseTls ? "wss" : "ws")}://{Host}:{Port}{Path}";
}
