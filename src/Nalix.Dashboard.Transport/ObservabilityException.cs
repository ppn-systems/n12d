// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Abstractions.Networking.Protocols;

namespace Nalix.Dashboard.Transport;

/// <summary>
/// The server refused or could not serve an observability request.
/// </summary>
public sealed class ObservabilityException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ObservabilityException"/> class.</summary>
    public ObservabilityException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ObservabilityException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public ObservabilityException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ObservabilityException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public ObservabilityException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ObservabilityException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="reason">The protocol reason returned by the server.</param>
    public ObservabilityException(string message, ProtocolReason reason) : base(message) => Reason = reason;

    /// <summary>Gets the protocol reason returned by the server, if any.</summary>
    public ProtocolReason Reason { get; }
}
