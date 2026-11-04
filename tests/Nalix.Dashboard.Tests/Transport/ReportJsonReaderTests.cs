// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Nalix.Dashboard.Abstractions.Reports;
using Nalix.Dashboard.Tests.TestSupport;
using Nalix.Dashboard.Transport;

namespace Nalix.Dashboard.Tests.Transport;

/// <summary>
/// Tests for <see cref="ReportJsonReader"/>.
/// </summary>
public sealed class ReportJsonReaderTests
{
    [Test]
    public void Parse_NestedPayload_BuildsTree()
    {
        ReportNode root = ReportFixtures.Parse("""{"Dispatch":{"Running":true,"TotalPackets":42,"Type":"x","Gone":null,"Items":[1,{"Name":"a"}]}}""");

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

    [Test]
    public void Parse_RepeatedNames_ReuseCachedStrings()
    {
        ReportJsonReader reader = new();
        byte[] json = Encoding.UTF8.GetBytes("""{"TotalConnections":1}""");
        ReportNode first = reader.Parse(json);
        ReportNode second = reader.Parse(json);
        Check.True(ReferenceEquals(first.Children[0].Name, second.Children[0].Name), "property name string is cached across parses");
    }

    [Test]
    public void Parse_EmptyPayload_ReturnsEmptyObject()
    {
        ReportNode root = new ReportJsonReader().Parse([]);
        Check.Equal(ReportNodeKind.Object, root.Kind);
        Check.Equal(0, root.Children.Count);
    }
}
