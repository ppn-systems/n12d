// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Connection;

/// <summary>
/// The lifecycle of the dashboard's connection to a Nalix server.
/// </summary>
public enum ConnectionStatus : byte
{
    /// <summary>No connection.</summary>
    Disconnected,

    /// <summary>Opening the WebSocket and running the handshake.</summary>
    Connecting,

    /// <summary>Presenting the access key.</summary>
    Authorizing,

    /// <summary>Connected with supervisor access; sampling is possible.</summary>
    Connected,

    /// <summary>The last attempt failed; see the error message.</summary>
    Faulted
}
