// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.State.Logging;

/// <summary>
/// A bounded, newest-first log of dashboard events.
/// </summary>
public sealed class EventLog
{
    private const int Capacity = 500;
    private readonly LinkedList<LogEntry> _entries = new();

    /// <summary>Gets the entries, newest first.</summary>
    public IReadOnlyCollection<LogEntry> Entries => _entries;

    /// <summary>
    /// Appends an entry, dropping the oldest when full.
    /// </summary>
    /// <param name="level">The severity.</param>
    /// <param name="message">The message.</param>
    public void Add(LogLevel level, string message)
    {
        _ = _entries.AddFirst(new LogEntry(DateTimeOffset.Now, level, message));
        if (_entries.Count > Capacity)
        {
            _entries.RemoveLast();
        }
    }

    /// <summary>Removes all entries.</summary>
    public void Clear() => _entries.Clear();
}
