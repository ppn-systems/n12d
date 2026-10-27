// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Microsoft.JSInterop;
using Nalix.Dashboard.Abstractions.Settings;

namespace Nalix.Dashboard.Infrastructure.Browser;

/// <summary>
/// <see cref="IFileSaver"/> that streams bytes to a Blob download (<c>wwwroot/js/n12d.js</c>).
/// </summary>
public sealed class BrowserFileSaver(IJSRuntime js) : IFileSaver
{
    /// <inheritdoc/>
    public async ValueTask SaveAsync(string fileName, string contentType, ReadOnlyMemory<byte> content)
    {
        using MemoryStream stream = new(content.ToArray(), writable: false);
        using DotNetStreamReference reference = new(stream);
        await js.InvokeVoidAsync("n12d.download", fileName, contentType, reference);
    }
}
