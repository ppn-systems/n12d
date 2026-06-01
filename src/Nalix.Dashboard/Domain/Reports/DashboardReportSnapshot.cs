// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Abstractions.Networking.Protocols;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Domain.Reports;

public sealed record DashboardReportSnapshot(
    RuntimeObservationTarget Target,
    ProtocolReason Reason,
    ReadOnlyMemory<byte> ObservationData,
    IReadOnlyDictionary<string, object?> Data,
    DateTimeOffset ReceivedAt);
