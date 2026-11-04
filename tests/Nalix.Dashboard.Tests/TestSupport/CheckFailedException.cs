// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Tests.TestSupport;

/// <summary>
/// Thrown by <see cref="Check"/> helpers when an assertion does not hold.
/// </summary>
internal sealed class CheckFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance with a failure message.
    /// </summary>
    /// <param name="message">The failure description.</param>
    public CheckFailedException(string message)
        : base(message)
    {
    }
}
