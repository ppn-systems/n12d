// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Dashboard.State.Metrics;
using Nalix.Dashboard.State.Recording;
using Nalix.Dashboard.Transport;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Tests;

/// <summary>
/// Tests for report parsing, metric history and recording.
/// </summary>
internal static class ReportTests
{
    public static void Register(List<(string Name, Action Body)> tests)
    {
        tests.Add(("Parse_NestedPayload_BuildsTree", Parse_NestedPayload_BuildsTree));
        tests.Add(("Parse_RepeatedNames_ReuseCachedStrings", Parse_RepeatedNames_ReuseCachedStrings));
        tests.Add(("Parse_EmptyPayload_ReturnsEmptyObject", Parse_EmptyPayload_ReturnsEmptyObject));
        tests.Add(("History_Record_FlattensLeavesAndKeysArrayItems", History_Record_FlattensLeavesAndKeysArrayItems));
        tests.Add(("History_TwoSamples_DerivesCpuAndAllocationRate", History_TwoSamples_DerivesCpuAndAllocationRate));
        tests.Add(("Series_OverCapacity_KeepsNewestInOrder", Series_OverCapacity_KeepsNewestInOrder));
        tests.Add(("Recorder_LateColumn_ExportsEmptyForEarlierRows", Recorder_LateColumn_ExportsEmptyForEarlierRows));
    }

    private static ReportNode Parse(string json) => new ReportJsonReader().Parse(Encoding.UTF8.GetBytes(json));

    private static void Parse_NestedPayload_BuildsTree()
    {
        ReportNode root = Parse("""{"Dispatch":{"Running":true,"TotalPackets":42,"Type":"x","Gone":null,"Items":[1,{"Name":"a"}]}}""");

        ReportNode dispatch = root.Get("Dispatch")!;
        Check.Equal(ReportNodeKind.Object, dispatch.Kind);
        Check.Equal(ReportNodeKind.Boolean, dispatch.Get("Running")!.Kind);
        Check.True(root.TryGetNumber("Dispatch.TotalPackets", out double packets), "number path resolves");
        Check.Near(42, packets, 0);
        Check.Equal("x", dispatch.Get("Type")!.Text);
        Check.Equal(ReportNodeKind.Null, dispatch.Get("Gone")!.Kind);

        ReportNode items = dispatch.Get("Items")!;
        Check.Equal(2, items.Children.Count);
        Check.Equal("1", items.Children[1].Name);
        Check.Equal("a", items.Children[1].Get("Name")!.Text);
    }

    private static void Parse_RepeatedNames_ReuseCachedStrings()
    {
        ReportJsonReader reader = new();
        byte[] json = Encoding.UTF8.GetBytes("""{"TotalConnections":1}""");
        ReportNode first = reader.Parse(json);
        ReportNode second = reader.Parse(json);
        Check.True(ReferenceEquals(first.Children[0].Name, second.Children[0].Name), "property name string is cached across parses");
    }

    private static void Parse_EmptyPayload_ReturnsEmptyObject()
    {
        ReportNode root = new ReportJsonReader().Parse([]);
        Check.Equal(ReportNodeKind.Object, root.Kind);
        Check.Equal(0, root.Children.Count);
    }

    private static void History_Record_FlattensLeavesAndKeysArrayItems()
    {
        MetricHistory history = new(10);
        history.Record(Snapshot(1000, """{"Dispatch":{"PipelineMetrics":{"TotalErrors":3},"MiddlewareMetrics":[{"Type":"Auth","TotalExecutions":7},{"TotalExecutions":9}]}}"""));

        Check.Near(3, history.Get("Dispatch.PipelineMetrics.TotalErrors")!.Latest, 0);
        Check.Near(7, history.Get("Dispatch.MiddlewareMetrics[Auth].TotalExecutions")!.Latest, 0);
        Check.Near(9, history.Get("Dispatch.MiddlewareMetrics[1].TotalExecutions")!.Latest, 0);
        Check.Near(12.5, history.Get(MetricPaths.RoundTripMs)!.Latest, 0);
    }

    private static void History_TwoSamples_DerivesCpuAndAllocationRate()
    {
        MetricHistory history = new(10);
        history.Record(Snapshot(10_000, """{"Process":{"CpuTimeMs":1000,"ProcessorCount":4,"TotalAllocatedBytes":0,"Gen0Collections":1,"Gen1Collections":0,"Gen2Collections":0}}"""));
        history.Record(Snapshot(11_000, """{"Process":{"CpuTimeMs":3000,"ProcessorCount":4,"TotalAllocatedBytes":5000,"Gen0Collections":3,"Gen1Collections":1,"Gen2Collections":0}}"""));

        // 2000 ms of CPU over 1000 ms wall on 4 cores = 50 %.
        Check.Near(50, history.Get(MetricPaths.CpuPercent)!.Latest, 1e-9);
        Check.Near(5000, history.Get(MetricPaths.AllocationBytesPerSecond)!.Latest, 1e-9);
        Check.Near(3, history.Get(MetricPaths.GcPerSecond)!.Latest, 1e-9);
    }

    private static void Series_OverCapacity_KeepsNewestInOrder()
    {
        MetricSeries series = new("x", 3);
        for (int i = 1; i <= 5; i++)
        {
            series.Add(i, i * 10);
        }

        Check.Equal(3, series.Count);
        Check.Near(30, series.Values[0], 0);
        Check.Near(50, series.Values[2], 0);
        Check.Equal(5L, series.Times[2]);
        Check.Near(50, series.Latest, 0);
    }

    private static void Recorder_LateColumn_ExportsEmptyForEarlierRows()
    {
        MetricHistory history = new(10);
        MetricRecorder recorder = new();
        recorder.Start();

        history.Record(Snapshot(1000, """{"Connections":{"TotalConnections":1}}"""));
        recorder.Capture(history, 1000);
        history.Record(Snapshot(2000, """{"Connections":{"TotalConnections":2,"ShardCount":4}}"""));
        recorder.Capture(history, 2000);

        string csv = Encoding.UTF8.GetString(recorder.ExportCsv());
        string[] lines = csv.TrimEnd('\n').Split('\n');
        Check.Equal(3, lines.Length);
        Check.Equal("timestamp_utc,Connections.ShardCount,Connections.TotalConnections,Dashboard.PayloadBytes,Dashboard.RoundTripMs", lines[0]);
        Check.True(lines[1].Contains(",,1,", StringComparison.Ordinal), "first row has an empty late column: " + lines[1]);
        Check.True(lines[2].Contains(",4,2,", StringComparison.Ordinal), "second row has both values: " + lines[2]);
    }

    private static ObservationSnapshot Snapshot(long sampledAt, string json)
        => new(sampledAt, RuntimeObservationTargets.ALL, Parse(json), json.Length, 12.5);
}
