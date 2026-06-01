// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Abstractions;

internal interface IReportParser
{
    RuntimeObservationTarget Target { get; }

    bool CanParse(RuntimeObservationTarget target);

    object? Parse(ReadOnlyMemory<byte> ObservationData);
}

internal interface IReportParser<TReport> : IReportParser where TReport : class
{
    TReport? ParseTyped(ReadOnlyMemory<byte> ObservationData);
}
