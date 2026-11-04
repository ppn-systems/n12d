// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Nalix.Dashboard.State.Metrics;
using Nalix.Dashboard.State.Recording;
using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests.State.Recording;

/// <summary>
/// Tests for <see cref="MetricRecorder"/>.
/// </summary>
public sealed class MetricRecorderTests
{
    [Test]
    public void ExportCsv_LateColumn_LeavesEarlierRowsEmpty()
    {
        MetricHistory history = new(10);
        MetricRecorder recorder = new();
        recorder.Start();

        history.Record(ReportFixtures.Snapshot(1000, """{"Connections":{"TotalConnections":1}}"""));
        recorder.Capture(history, 1000);
        history.Record(ReportFixtures.Snapshot(2000, """{"Connections":{"TotalConnections":2,"ShardCount":4}}"""));
        recorder.Capture(history, 2000);

        string csv = Encoding.UTF8.GetString(recorder.ExportCsv());
        string[] lines = csv.TrimEnd('\n').Split('\n');
        Check.Equal(3, lines.Length);
        Check.Equal("timestamp_utc,Connections.ShardCount,Connections.TotalConnections,Dashboard.PayloadBytes,Dashboard.RoundTripMs", lines[0]);
        Check.True(lines[1].Contains(",,1,", StringComparison.Ordinal), "first row has an empty late column: " + lines[1]);
        Check.True(lines[2].Contains(",4,2,", StringComparison.Ordinal), "second row has both values: " + lines[2]);
    }
}
