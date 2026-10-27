// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Tests;

/// <summary>
/// Entry point of the dependency-free test runner. Exit code 0 means every test passed.
/// </summary>
internal static class Program
{
    private static int Main()
    {
        List<(string Name, Action Body)> tests = new();
        StatisticsTests.Register(tests);
        CsvTests.Register(tests);
        ReportTests.Register(tests);

        int passed = 0;
        int failed = 0;

        foreach ((string Name, Action Body) test in tests)
        {
            try
            {
                test.Body();
                passed++;
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                string message = ex is CheckFailedException
                    ? ex.Message
                    : $"{ex.GetType().Name}: {ex.Message}";
                Console.WriteLine($"FAIL {test.Name}: {message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {passed} passed, {failed} failed, {tests.Count} total");
        return failed == 0 ? 0 : 1;
    }
}
