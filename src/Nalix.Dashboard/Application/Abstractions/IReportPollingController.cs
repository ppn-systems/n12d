// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Abstractions;

internal interface IReportPollingController
{
    RuntimeObservationTarget? ActiveTarget { get; }

    string? ActiveRoute { get; }

    bool IsPolling { get; }

    bool IsPaused { get; }

    int CurrentIntervalMs { get; }

    DateTimeOffset? LastRequestUtc { get; }

    DateTimeOffset? LastSuccessUtc { get; }

    DateTimeOffset? LastFailureUtc { get; }

    string? LastFailureMessage { get; }

    event Action? Changed;

    void Activate(RuntimeObservationTarget target, string route, int intervalMs);

    void Deactivate(string route);

    void Pause();

    void Resume();

    void SetInterval(int intervalMs);

    Task RequestOnceAsync(RuntimeObservationTarget target, CancellationToken ct);
}
