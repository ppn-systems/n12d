// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Reports;

/// <summary>
/// An immutable JSON value from a runtime report. Reports are rendered generically from this tree,
/// so new fields on the server appear in the dashboard without code changes.
/// </summary>
public sealed class ReportNode
{
    private static readonly ReportNode[] s_none = [];

    private ReportNode(string name, ReportNodeKind kind, double number, string? text, ReportNode[] children)
    {
        Name = name;
        Kind = kind;
        Number = number;
        Text = text;
        Children = children;
    }

    /// <summary>Gets the property name, or the item index as text for array items.</summary>
    public string Name { get; }

    /// <summary>Gets the value kind.</summary>
    public ReportNodeKind Kind { get; }

    /// <summary>Gets the numeric value for <see cref="ReportNodeKind.Number"/>, 1/0 for booleans, otherwise 0.</summary>
    public double Number { get; }

    /// <summary>Gets the string value for <see cref="ReportNodeKind.String"/>; otherwise <see langword="null"/>.</summary>
    public string? Text { get; }

    /// <summary>Gets object properties or array items; empty for scalars.</summary>
    public IReadOnlyList<ReportNode> Children { get; }

    /// <summary>Gets a value indicating whether the node is an object or array.</summary>
    public bool IsContainer => Kind is ReportNodeKind.Object or ReportNodeKind.Array;

    /// <summary>Creates a scalar number node.</summary>
    public static ReportNode CreateNumber(string name, double value) => new(name, ReportNodeKind.Number, value, null, s_none);

    /// <summary>Creates a scalar string node.</summary>
    public static ReportNode CreateString(string name, string value) => new(name, ReportNodeKind.String, 0, value, s_none);

    /// <summary>Creates a scalar boolean node.</summary>
    public static ReportNode CreateBoolean(string name, bool value) => new(name, ReportNodeKind.Boolean, value ? 1 : 0, null, s_none);

    /// <summary>Creates a null node.</summary>
    public static ReportNode CreateNull(string name) => new(name, ReportNodeKind.Null, 0, null, s_none);

    /// <summary>Creates an object node.</summary>
    public static ReportNode CreateObject(string name, ReportNode[] properties) => new(name, ReportNodeKind.Object, 0, null, properties);

    /// <summary>Creates an array node.</summary>
    public static ReportNode CreateArray(string name, ReportNode[] items) => new(name, ReportNodeKind.Array, 0, null, items);

    /// <summary>
    /// Finds a direct child property by name (ordinal, case-sensitive).
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The child, or <see langword="null"/> when absent.</returns>
    public ReportNode? Get(string name)
    {
        IReadOnlyList<ReportNode> children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (string.Equals(children[i].Name, name, StringComparison.Ordinal))
            {
                return children[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a dot-separated property path such as <c>PipelineMetrics.TotalErrors</c>.
    /// </summary>
    /// <param name="path">The path relative to this node.</param>
    /// <returns>The node, or <see langword="null"/> when any segment is missing.</returns>
    public ReportNode? Find(string path)
    {
        ReportNode? current = this;
        foreach (Range segment in path.AsSpan().Split('.'))
        {
            current = current.Get(path[segment]);
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Reads a numeric value at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path relative to this node.</param>
    /// <param name="value">Receives the value when found.</param>
    /// <returns><see langword="true"/> when the path resolves to a number or boolean.</returns>
    public bool TryGetNumber(string path, out double value)
    {
        ReportNode? node = Find(path);
        if (node is { Kind: ReportNodeKind.Number or ReportNodeKind.Boolean })
        {
            value = node.Number;
            return true;
        }

        value = 0;
        return false;
    }
}
