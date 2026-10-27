// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;

namespace Nalix.Dashboard.Tests;

/// <summary>
/// Thrown by <see cref="Check"/> helpers when an assertion does not hold.
/// </summary>
internal sealed class CheckFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance with a failure message.
    /// </summary>
    /// <param name="message">The failure description.</param>
    public CheckFailedException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Minimal assertion helpers for the dependency-free test runner.
/// </summary>
internal static class Check
{
    /// <summary>
    /// Asserts that a condition holds.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="message">The message reported when the condition is false.</param>
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new CheckFailedException(message);
        }
    }

    /// <summary>
    /// Asserts that two values are equal using the default equality comparer.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new CheckFailedException(string.Format(
                CultureInfo.InvariantCulture,
                "expected <{0}> but was <{1}>",
                expected,
                actual));
        }
    }

    /// <summary>
    /// Asserts that two doubles differ by at most <paramref name="tolerance"/>. Two NaNs are considered equal.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="tolerance">The maximum allowed absolute difference.</param>
    public static void Near(double expected, double actual, double tolerance)
    {
        if (double.IsNaN(expected) && double.IsNaN(actual))
        {
            return;
        }

        if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new CheckFailedException(string.Format(
                CultureInfo.InvariantCulture,
                "expected {0} ± {1} but was {2}",
                expected,
                tolerance,
                actual));
        }
    }

    /// <summary>
    /// Asserts that <paramref name="body"/> throws an exception of type <typeparamref name="TException"/> (or a derived type).
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="body">The code expected to throw.</param>
    public static void Throws<TException>(Action body)
        where TException : Exception
    {
        try
        {
            body();
        }
        catch (Exception ex)
        {
            if (ex is TException)
            {
                return;
            }

            throw new CheckFailedException(
                $"expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }

        throw new CheckFailedException($"expected {typeof(TException).Name} but nothing was thrown");
    }
}
