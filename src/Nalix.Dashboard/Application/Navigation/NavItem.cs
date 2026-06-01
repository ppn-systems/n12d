// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Nalix.Abstractions.Security;

namespace Nalix.Dashboard.Application.Navigation;

public sealed record NavItem(
    string Title,
    string Route,
    string Icon,
    PermissionLevel RequiredLevel = PermissionLevel.NONE,
    bool IsEnabled = true,
    bool IsVisible = true,
    string? Badge = null);

public sealed record NavSection(string Title, IReadOnlyList<NavItem> Items);
