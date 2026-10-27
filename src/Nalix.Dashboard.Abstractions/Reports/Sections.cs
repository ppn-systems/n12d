// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Abstractions.Reports;

/// <summary>
/// The catalog of report sections the dashboard knows how to request and display.
/// </summary>
public static class Sections
{
    /// <summary>Process counters.</summary>
    public static readonly SectionInfo Process = new(RuntimeObservationTargets.PROCESS, RuntimeObservationSections.Process, "process", "Process", "CPU, memory, garbage collector and thread pool.");

    /// <summary>Packet dispatch.</summary>
    public static readonly SectionInfo Dispatch = new(RuntimeObservationTargets.DISPATCH, RuntimeObservationSections.Dispatch, "dispatch", "Dispatch", "Packet queues, pipeline timings and middleware.");

    /// <summary>Task manager.</summary>
    public static readonly SectionInfo Tasks = new(RuntimeObservationTargets.TASKS, RuntimeObservationSections.Tasks, "tasks", "Tasks", "Workers, recurring jobs and concurrency limits.");

    /// <summary>Buffer pools.</summary>
    public static readonly SectionInfo Buffers = new(RuntimeObservationTargets.BUFFERS, RuntimeObservationSections.Buffers, "buffers", "Buffers", "Buffer pool usage and allocation.");

    /// <summary>Connections.</summary>
    public static readonly SectionInfo Connections = new(RuntimeObservationTargets.CONNECTIONS, RuntimeObservationSections.Connections, "connections", "Connections", "Connection hub traffic, callbacks and timing wheel.");

    /// <summary>Sessions.</summary>
    public static readonly SectionInfo Sessions = new(RuntimeObservationTargets.SESSIONS, RuntimeObservationSections.Sessions, "sessions", "Sessions", "Session store and resume statistics.");

    /// <summary>Instances.</summary>
    public static readonly SectionInfo Instances = new(RuntimeObservationTargets.INSTANCES, RuntimeObservationSections.Instances, "instances", "Instances", "Registered singletons and services.");

    /// <summary>Object pools.</summary>
    public static readonly SectionInfo ObjectPools = new(RuntimeObservationTargets.OBJECT_POOLS, RuntimeObservationSections.ObjectPools, "object-pools", "Object pools", "Pooled object usage and lifecycle.");

    /// <summary>Connection guard.</summary>
    public static readonly SectionInfo ConnectionGuard = new(RuntimeObservationTargets.CONNECTION_GUARD, RuntimeObservationSections.ConnectionGuard, "connection-guard", "Connection guard", "Per-address throttling and bans.");

    /// <summary>Concurrency gate.</summary>
    public static readonly SectionInfo ConcurrencyGate = new(RuntimeObservationTargets.CONCURRENCY_GATE, RuntimeObservationSections.ConcurrencyGate, "concurrency-gate", "Concurrency gate", "Concurrency limits and queueing.");

    /// <summary>Policy rate limiter.</summary>
    public static readonly SectionInfo PolicyRateLimiter = new(RuntimeObservationTargets.POLICY_RATE_LIMITER, RuntimeObservationSections.PolicyRateLimiter, "policy-rate-limiter", "Policy limiter", "Policy-based rate limiting.");

    /// <summary>Token bucket limiter.</summary>
    public static readonly SectionInfo TokenBucketLimiter = new(RuntimeObservationTargets.TOKEN_BUCKET_LIMITER, RuntimeObservationSections.TokenBucketLimiter, "token-bucket-limiter", "Token bucket", "Token bucket rate limiting.");

    /// <summary>Listeners.</summary>
    public static readonly SectionInfo Listeners = new(RuntimeObservationTargets.LISTENERS, RuntimeObservationSections.Listeners, "listeners", "Listeners", "TCP, UDP and WebSocket listeners.");

    /// <summary>Protocols.</summary>
    public static readonly SectionInfo Protocols = new(RuntimeObservationTargets.PROTOCOLS, RuntimeObservationSections.Protocols, "protocols", "Protocols", "Per-transport protocol metrics.");

    /// <summary>Gets every section in navigation order.</summary>
    public static IReadOnlyList<SectionInfo> All { get; } =
    [
        Process, Dispatch, Connections, Sessions, Tasks, Buffers, ObjectPools,
        Listeners, Protocols, ConnectionGuard, ConcurrencyGate, PolicyRateLimiter, TokenBucketLimiter, Instances,
    ];

    /// <summary>
    /// Finds a section by its route slug.
    /// </summary>
    /// <param name="slug">The URL segment.</param>
    /// <returns>The section, or <see langword="null"/> when unknown.</returns>
    public static SectionInfo? FindBySlug(string? slug)
    {
        foreach (SectionInfo section in All)
        {
            if (string.Equals(section.Slug, slug, StringComparison.OrdinalIgnoreCase))
            {
                return section;
            }
        }

        return null;
    }
}
