// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.State.Logging;

/// <summary>
/// Severity of a dashboard event.
/// </summary>
public enum LogLevel : byte
{
    /// <summary>Informational.</summary>
    Info,

    /// <summary>Recoverable problem.</summary>
    Warning,

    /// <summary>Failure.</summary>
    Error
}

/// <summary>
/// One event in the dashboard's own activity log (connections, failures, recordings).
/// </summary>
/// <param name="At">When it happened.</param>
/// <param name="Level">The severity.</param>
/// <param name="Message">The message.</param>
public sealed record LogEntry(DateTimeOffset At, LogLevel Level, string Message);
