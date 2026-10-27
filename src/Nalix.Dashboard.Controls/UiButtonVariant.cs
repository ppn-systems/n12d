// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Controls;

/// <summary>
/// The visual weight of a <see cref="UiButton"/>.
/// </summary>
public enum UiButtonVariant
{
    /// <summary>The main call to action, filled with the accent colour.</summary>
    Primary,

    /// <summary>A bordered button on the surface colour.</summary>
    Secondary,

    /// <summary>A text-only button for low-emphasis actions.</summary>
    Ghost,

    /// <summary>A destructive action, filled with the danger colour.</summary>
    Danger
}
