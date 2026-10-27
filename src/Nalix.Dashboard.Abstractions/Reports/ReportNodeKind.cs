// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Reports;

/// <summary>
/// The JSON value kind of a <see cref="ReportNode"/>.
/// </summary>
public enum ReportNodeKind : byte
{
    /// <summary>JSON <c>null</c>.</summary>
    Null,

    /// <summary>JSON <c>true</c> or <c>false</c>.</summary>
    Boolean,

    /// <summary>A JSON number.</summary>
    Number,

    /// <summary>A JSON string.</summary>
    String,

    /// <summary>A JSON object; <see cref="ReportNode.Children"/> holds its properties in document order.</summary>
    Object,

    /// <summary>A JSON array; <see cref="ReportNode.Children"/> holds its items.</summary>
    Array
}
