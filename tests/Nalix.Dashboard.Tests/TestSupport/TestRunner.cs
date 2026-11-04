// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Reflection;

namespace Nalix.Dashboard.Tests.TestSupport;

/// <summary>
/// Dependency-free test runner. Discovers every public, non-abstract class whose name ends with <c>Tests</c> and
/// every public instance method marked with <see cref="TestAttribute"/> in the test assembly, then runs them.
/// Each test method runs on a new instance of its class, which is disposed afterwards.
/// </summary>
internal static class TestRunner
{
    private const string ClassSuffix = "Tests";

    /// <summary>
    /// Discovers and runs the tests, printing one PASS, FAIL or SKIP line per test and a summary line.
    /// </summary>
    /// <param name="filter">Optional substring matched against the full test name (<c>Namespace.Class.Method</c>).</param>
    /// <returns>0 when every selected test passed or was skipped; 1 otherwise, including when no test matched.</returns>
    public static async Task<int> RunAsync(string? filter)
    {
        List<TestCase> cases = Discover(typeof(TestRunner).Assembly, filter);
        if (cases.Count == 0)
        {
            Console.Error.WriteLine("No tests matched the filter.");
            return 1;
        }

        int passed = 0;
        int failed = 0;
        int skipped = 0;

        foreach (TestCase test in cases)
        {
            string name = test.FullName;
            if (test.Attribute.Skip is not null)
            {
                skipped++;
                Console.WriteLine($"SKIP {name}: {test.Attribute.Skip}");
                continue;
            }

            Exception? failure = await ExecuteAsync(test);
            if (failure is null)
            {
                passed++;
                Console.WriteLine($"PASS {name}");
            }
            else
            {
                failed++;
                Console.WriteLine($"FAIL {name}: {Describe(failure)}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {passed} passed, {failed} failed, {skipped} skipped, {cases.Count} total");
        return failed == 0 ? 0 : 1;
    }

    private static List<TestCase> Discover(Assembly assembly, string? filter)
    {
        Type[] types = assembly.GetTypes();
        Array.Sort(types, static (a, b) => string.CompareOrdinal(a.FullName, b.FullName));

        List<TestCase> cases = new();
        foreach (Type type in types)
        {
            if (!type.IsClass || !type.IsPublic || type.IsAbstract || !type.Name.EndsWith(ClassSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Array.Sort(methods, static (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

            foreach (MethodInfo method in methods)
            {
                TestAttribute? attribute = method.GetCustomAttribute<TestAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                TestCase test = new(type, method, attribute);
                if (string.IsNullOrEmpty(filter) || test.FullName.Contains(filter, StringComparison.Ordinal))
                {
                    cases.Add(test);
                }
            }
        }

        return cases;
    }

    /// <summary>
    /// Runs one test on a fresh instance and disposes that instance.
    /// </summary>
    /// <returns>The failure, or null when the test passed.</returns>
    private static async Task<Exception?> ExecuteAsync(TestCase test)
    {
        object? instance = null;
        Exception? failure = null;
        try
        {
            instance = Activator.CreateInstance(test.Type);
            await InvokeAsync(test.Method, instance);
        }
        catch (Exception ex)
        {
            // Reflection wraps exceptions thrown synchronously by the test body; report the original.
            failure = ex is TargetInvocationException { InnerException: not null } wrapped ? wrapped.InnerException : ex;
        }

        Exception? disposeFailure = await DisposeAsync(instance);
        return failure ?? disposeFailure;
    }

    private static async Task InvokeAsync(MethodInfo method, object? instance)
    {
        Type returnType = method.ReturnType;
        if (method.GetParameters().Length != 0 || (returnType != typeof(void) && returnType != typeof(Task)))
        {
            throw new InvalidOperationException("a [Test] method must take no parameters and return void or Task");
        }

        object? result = method.Invoke(instance, null);
        if (result is Task task)
        {
            await task;
        }
    }

    private static async Task<Exception?> DisposeAsync(object? instance)
    {
        try
        {
            if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static string Describe(Exception failure)
    {
        return failure is CheckFailedException
            ? failure.Message
            : $"{failure.GetType().Name}: {failure.Message}";
    }

    private sealed record TestCase(Type Type, MethodInfo Method, TestAttribute Attribute)
    {
        public string FullName => $"{Type.FullName}.{Method.Name}";
    }
}
