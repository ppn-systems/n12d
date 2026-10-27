// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using System.Text.Json;
using Nalix.Dashboard.Abstractions.Reports;

namespace Nalix.Dashboard.Transport;

/// <summary>
/// Parses a UTF-8 JSON report into an immutable <see cref="ReportNode"/> tree.
/// </summary>
/// <remarks>
/// The same property names arrive on every sample, so names are decoded onto the stack and
/// resolved through a cache instead of allocating a new string per property per sample.
/// One instance is meant to be reused by a single caller; it is not thread-safe.
/// </remarks>
public sealed class ReportJsonReader
{
    private const int MaxCachedNameLength = 128;
    private const int MaxCachedNames = 4096;

    private static readonly string[] s_indexNames = CREATE_INDEX_NAMES(256);
    private static readonly JsonReaderOptions s_options = new() { MaxDepth = 64, CommentHandling = JsonCommentHandling.Skip };

    private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _lookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportJsonReader"/> class.
    /// </summary>
    public ReportJsonReader() => _lookup = _names.GetAlternateLookup<ReadOnlySpan<char>>();

    /// <summary>
    /// Parses <paramref name="utf8"/> into a tree whose root is named <paramref name="rootName"/>.
    /// </summary>
    /// <param name="utf8">The UTF-8 JSON document.</param>
    /// <param name="rootName">The name given to the root node.</param>
    /// <returns>The root node; an empty object for empty input.</returns>
    /// <exception cref="JsonException">The payload is not valid JSON.</exception>
    public ReportNode Parse(ReadOnlySpan<byte> utf8, string rootName = "")
    {
        if (utf8.IsEmpty)
        {
            return ReportNode.CreateObject(rootName, []);
        }

        Utf8JsonReader reader = new(utf8, s_options);
        if (!reader.Read())
        {
            return ReportNode.CreateObject(rootName, []);
        }

        return READ_VALUE(ref reader, rootName);
    }

    private ReportNode READ_VALUE(ref Utf8JsonReader reader, string name)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
            {
                List<ReportNode> properties = [];
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string propertyName = READ_NAME(ref reader);
                    _ = reader.Read();
                    properties.Add(READ_VALUE(ref reader, propertyName));
                }

                return ReportNode.CreateObject(name, [.. properties]);
            }

            case JsonTokenType.StartArray:
            {
                List<ReportNode> items = [];
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    int index = items.Count;
                    items.Add(READ_VALUE(ref reader, index < s_indexNames.Length ? s_indexNames[index] : index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }

                return ReportNode.CreateArray(name, [.. items]);
            }

            case JsonTokenType.Number:
                return ReportNode.CreateNumber(name, reader.GetDouble());

            case JsonTokenType.String:
                return ReportNode.CreateString(name, reader.GetString() ?? string.Empty);

            case JsonTokenType.True:
                return ReportNode.CreateBoolean(name, true);

            case JsonTokenType.False:
                return ReportNode.CreateBoolean(name, false);

            case JsonTokenType.Null:
            default:
                return ReportNode.CreateNull(name);
        }
    }

    private string READ_NAME(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped || reader.ValueSpan.Length > MaxCachedNameLength)
        {
            return reader.GetString() ?? string.Empty;
        }

        ReadOnlySpan<byte> raw = reader.ValueSpan;
        Span<char> chars = stackalloc char[MaxCachedNameLength];
        int length = Encoding.UTF8.GetChars(raw, chars);
        ReadOnlySpan<char> key = chars[..length];

        if (_lookup.TryGetValue(key, out string? cached))
        {
            return cached;
        }

        string created = new(key);
        if (_names.Count < MaxCachedNames)
        {
            _names[created] = created;
        }

        return created;
    }

    private static string[] CREATE_INDEX_NAMES(int count)
    {
        string[] names = new string[count];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return names;
    }
}
