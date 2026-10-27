// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;
using Nalix.Dashboard.Abstractions.Settings;

namespace Nalix.Dashboard.Infrastructure.Browser;

/// <summary>
/// Source-generated JSON metadata for persisted settings (trim-safe, no reflection).
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DashboardSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
