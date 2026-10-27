// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Nalix.Dashboard.Abstractions.Reports;

namespace Nalix.Dashboard.Pages.Shared;

/// <summary>
/// Renders a <see cref="ReportNode"/> back to indented JSON for the raw view.
/// </summary>
public static class ReportText
{
    /// <summary>Appends <paramref name="node"/> as indented JSON.</summary>
    /// <param name="builder">The target.</param>
    /// <param name="node">The node.</param>
    /// <param name="depth">The current indentation depth.</param>
    public static void Write(StringBuilder builder, ReportNode node, int depth)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(node);

        switch (node.Kind)
        {
            case ReportNodeKind.Object:
            case ReportNodeKind.Array:
                bool isObject = node.Kind == ReportNodeKind.Object;
                if (node.Children.Count == 0)
                {
                    _ = builder.Append(isObject ? "{}" : "[]");
                    return;
                }

                _ = builder.Append(isObject ? '{' : '[').Append('\n');
                for (int i = 0; i < node.Children.Count; i++)
                {
                    ReportNode child = node.Children[i];
                    _ = builder.Append(' ', (depth + 1) * 2);
                    if (isObject)
                    {
                        _ = builder.Append('"').Append(JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(child.Name)).Append("\": ");
                    }

                    Write(builder, child, depth + 1);
                    _ = builder.Append(i < node.Children.Count - 1 ? ",\n" : "\n");
                }

                _ = builder.Append(' ', depth * 2).Append(isObject ? '}' : ']');
                break;

            case ReportNodeKind.Number:
                _ = builder.Append(node.Number.ToString("R", CultureInfo.InvariantCulture));
                break;

            case ReportNodeKind.String:
                _ = builder.Append('"').Append(JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(node.Text ?? string.Empty)).Append('"');
                break;

            case ReportNodeKind.Boolean:
                _ = builder.Append(node.Number != 0 ? "true" : "false");
                break;

            default:
                _ = builder.Append("null");
                break;
        }
    }
}
