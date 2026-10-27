// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Collections;

namespace Nalix.Dashboard.State.Metrics;

/// <summary>
/// A fixed-capacity ring buffer of timestamped samples for one metric.
/// </summary>
/// <remarks>
/// <see cref="Values"/> and <see cref="Times"/> are allocation-free live views in chronological order,
/// so charts can bind to them directly on every render.
/// </remarks>
public sealed class MetricSeries
{
    private readonly long[] _times;
    private readonly double[] _values;
    private int _start;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricSeries"/> class.
    /// </summary>
    /// <param name="path">The metric path.</param>
    /// <param name="capacity">The number of samples kept.</param>
    public MetricSeries(string path, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);
        Path = path;
        _times = new long[capacity];
        _values = new double[capacity];
        Values = new View<double>(this, _values);
        Times = new View<long>(this, _times);
    }

    /// <summary>Gets the metric path, e.g. <c>Process.WorkingSetBytes</c>.</summary>
    public string Path { get; }

    /// <summary>Gets the number of samples held.</summary>
    public int Count { get; private set; }

    /// <summary>Gets the capacity.</summary>
    public int Capacity => _values.Length;

    /// <summary>Gets the values, oldest first.</summary>
    public IReadOnlyList<double> Values { get; }

    /// <summary>Gets the Unix-millisecond times, oldest first.</summary>
    public IReadOnlyList<long> Times { get; }

    /// <summary>Gets the most recent value, or NaN when empty.</summary>
    public double Latest => Count == 0 ? double.NaN : _values[INDEX(Count - 1)];

    /// <summary>Gets the time of the most recent value, or 0 when empty.</summary>
    public long LatestTime => Count == 0 ? 0 : _times[INDEX(Count - 1)];

    /// <summary>
    /// Appends a sample, overwriting the oldest when full.
    /// </summary>
    /// <param name="time">The Unix-millisecond time.</param>
    /// <param name="value">The value.</param>
    public void Add(long time, double value)
    {
        if (Count < _values.Length)
        {
            int index = INDEX(Count);
            _times[index] = time;
            _values[index] = value;
            Count++;
            return;
        }

        _times[_start] = time;
        _values[_start] = value;
        _start = (_start + 1) % _values.Length;
    }

    /// <summary>
    /// Copies samples into spans, oldest first.
    /// </summary>
    /// <param name="times">Receives times; must hold <see cref="Count"/> items.</param>
    /// <param name="values">Receives values; must hold <see cref="Count"/> items.</param>
    public void CopyTo(Span<long> times, Span<double> values)
    {
        for (int i = 0; i < Count; i++)
        {
            int index = INDEX(i);
            times[i] = _times[index];
            values[i] = _values[index];
        }
    }

    private int INDEX(int offset) => (_start + offset) % _values.Length;

    private sealed class View<T>(MetricSeries owner, T[] buffer) : IReadOnlyList<T>
    {
        public int Count => owner.Count;

        public T this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)owner.Count, nameof(index));
                return buffer[owner.INDEX(index)];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < owner.Count; i++)
            {
                yield return buffer[owner.INDEX(i)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
