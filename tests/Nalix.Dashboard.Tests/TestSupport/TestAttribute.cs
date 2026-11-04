// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Tests.TestSupport;

/// <summary>
/// Marks a public instance method of a public <c>*Tests</c> class as a test. <see cref="TestRunner"/> discovers and runs it.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TestAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the reason this test is skipped. When set, the test is reported as SKIP and never runs.
    /// </summary>
    public string? Skip { get; set; }
}
