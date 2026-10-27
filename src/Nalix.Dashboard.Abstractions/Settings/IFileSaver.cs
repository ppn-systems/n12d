// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Abstractions.Settings;

/// <summary>
/// Hands a generated file to the browser as a download.
/// </summary>
public interface IFileSaver
{
    /// <summary>Downloads <paramref name="content"/> as <paramref name="fileName"/>.</summary>
    /// <param name="fileName">The suggested file name.</param>
    /// <param name="contentType">The MIME type.</param>
    /// <param name="content">The file bytes.</param>
    /// <returns>A task completing when the download is triggered.</returns>
    ValueTask SaveAsync(string fileName, string contentType, ReadOnlyMemory<byte> content);
}
