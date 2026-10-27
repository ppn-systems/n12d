// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Abstractions.History;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Abstractions.Connection;

/// <summary>
/// The transport to a Nalix server running <c>Nalix.Observability</c>.
/// </summary>
public interface IObservabilityClient : IAsyncDisposable
{
    /// <summary>Raised when the connection drops unexpectedly; the argument is the reason.</summary>
    event Action<string>? Disconnected;

    /// <summary>Gets a value indicating whether the client is connected and authorized.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects, performs the Nalix handshake and presents the access key.
    /// </summary>
    /// <param name="endpoint">The server endpoint.</param>
    /// <param name="accessKey">The 64-character hex access key.</param>
    /// <param name="progress">Receives status transitions while connecting.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task completing once the connection is authorized.</returns>
    Task ConnectAsync(ServerEndpoint endpoint, string accessKey, Action<ConnectionStatus>? progress, CancellationToken ct);

    /// <summary>Closes the connection.</summary>
    /// <returns>A task completing when closed.</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Requests one snapshot of the selected sections.
    /// </summary>
    /// <param name="targets">The sections to sample.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The parsed snapshot.</returns>
    Task<ObservationSnapshot> SampleAsync(RuntimeObservationTargets targets, CancellationToken ct);

    /// <summary>
    /// Lists the days and metrics the server has recorded between <paramref name="from"/> and <paramref name="to"/>.
    /// </summary>
    /// <param name="from">Range start, Unix milliseconds.</param>
    /// <param name="to">Range end, Unix milliseconds.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The catalog.</returns>
    Task<HistoryCatalog> GetHistoryCatalogAsync(long from, long to, CancellationToken ct);

    /// <summary>
    /// Reads recorded history of <paramref name="metrics"/>, folded by the server into at most <paramref name="points"/> buckets.
    /// </summary>
    /// <param name="from">Range start, Unix milliseconds.</param>
    /// <param name="to">Range end, Unix milliseconds.</param>
    /// <param name="points">Maximum buckets per metric (≤ 2000).</param>
    /// <param name="metrics">Metric paths (≤ 32).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The series.</returns>
    Task<HistorySeries> GetHistoryAsync(long from, long to, int points, IReadOnlyList<string> metrics, CancellationToken ct);
}
