// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.State.Metrics;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.State.Metrics;

/// <summary>
/// Tests for <see cref="MetricHistory"/>.
/// </summary>
public sealed class MetricHistoryTests
{
    [Test]
    public void Record_NestedPayload_FlattensLeavesAndKeysArrayItems()
    {
        MetricHistory history = new(10);
        history.Record(ReportFixtures.Snapshot(1000, """{"Dispatch":{"PipelineMetrics":{"TotalErrors":3},"MiddlewareMetrics":[{"Type":"Auth","TotalExecutions":7},{"TotalExecutions":9}]}}"""));

        Check.Near(3, history.Get("Dispatch.PipelineMetrics.TotalErrors")!.Latest, 0);
        Check.Near(7, history.Get("Dispatch.MiddlewareMetrics[Auth].TotalExecutions")!.Latest, 0);
        Check.Near(9, history.Get("Dispatch.MiddlewareMetrics[1].TotalExecutions")!.Latest, 0);
        Check.Near(12.5, history.Get(MetricPaths.RoundTripMs)!.Latest, 0);
    }

    [Test]
    public void Record_TwoSamples_DerivesCpuAndAllocationRate()
    {
        MetricHistory history = new(10);
        history.Record(ReportFixtures.Snapshot(10_000, """{"Process":{"CpuTimeMs":1000,"ProcessorCount":4,"TotalAllocatedBytes":0,"Gen0Collections":1,"Gen1Collections":0,"Gen2Collections":0}}"""));
        history.Record(ReportFixtures.Snapshot(11_000, """{"Process":{"CpuTimeMs":3000,"ProcessorCount":4,"TotalAllocatedBytes":5000,"Gen0Collections":3,"Gen1Collections":1,"Gen2Collections":0}}"""));

        // 2000 ms of CPU over 1000 ms wall on 4 cores = 50 %.
        Check.Near(50, history.Get(MetricPaths.CpuPercent)!.Latest, 1e-9);
        Check.Near(5000, history.Get(MetricPaths.AllocationBytesPerSecond)!.Latest, 1e-9);
        Check.Near(3, history.Get(MetricPaths.GcPerSecond)!.Latest, 1e-9);
    }
}
