// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Dashboard.Tests.TestSupport;

namespace Nalix.Dashboard.Tests;

/// <summary>
/// Entry point of the dependency-free test runner. Exit code 0 means every selected test passed or was skipped.
/// The first argument, when present, is a substring filter on the full test name.
/// </summary>
internal static class Program
{
    private static Task<int> Main(string[] args)
    {
        return TestRunner.RunAsync(args.Length > 0 ? args[0] : null);
    }
}
