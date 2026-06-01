using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Nalix.Dashboard.Infrastructure.Serialization;
using Nalix.Observability.Contracts;

namespace Nalix.Dashboard.Application.Services;

internal static class RecordingExporter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public static byte[] ExportToZipJson(RuntimeObservationTarget target, IReadOnlyList<RecordedSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var frames = snapshots.Select(s => new
        {
            timestamp = s.Timestamp,
            data = ReportJsonParser.Parse(s.ObservationData)
        }).ToList();

        string jsonText = JsonSerializer.Serialize(frames, s_jsonOptions);

        using var outputStream = new MemoryStream();
        using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
        {
            var fileName = $"{target.ToString().ToLowerInvariant()}_metrics.json";
            var entry = archive.CreateEntry(fileName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(jsonText);
        }

        return outputStream.ToArray();
    }

    public static byte[] ExportAllToZipJson(IReadOnlyDictionary<RuntimeObservationTarget, IReadOnlyList<RecordedSnapshot>> data)
    {
        if (data == null || data.Count == 0)
        {
            return Array.Empty<byte>();
        }

        using var outputStream = new MemoryStream();
        using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
        {
            foreach (var kvp in data)
            {
                var target = kvp.Key;
                var snapshots = kvp.Value;
                if (snapshots == null || snapshots.Count == 0)
                {
                    continue;
                }

                string targetName = target.ToString().ToLowerInvariant();

                // Write JSON file
                var frames = snapshots.Select(s => new
                {
                    timestamp = s.Timestamp,
                    data = ReportJsonParser.Parse(s.ObservationData)
                }).ToList();
                string jsonText = JsonSerializer.Serialize(frames, s_jsonOptions);

                var jsonEntry = archive.CreateEntry($"{targetName}_metrics.json");
                using (var jsonStream = jsonEntry.Open())
                using (var writer = new StreamWriter(jsonStream, Encoding.UTF8))
                {
                    writer.Write(jsonText);
                }
            }
        }

        return outputStream.ToArray();
    }

    public static byte[] ExportAllToZipCsv(IReadOnlyDictionary<RuntimeObservationTarget, IReadOnlyList<RecordedSnapshot>> data)
    {
        if (data == null || data.Count == 0)
        {
            return Array.Empty<byte>();
        }

        using var outputStream = new MemoryStream();
        using (var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
        {
            foreach (var kvp in data)
            {
                var target = kvp.Key;
                var snapshots = kvp.Value;
                if (snapshots == null || snapshots.Count == 0)
                {
                    continue;
                }

                string targetName = target.ToString().ToLowerInvariant();

                // Write CSV file
                byte[] csvBytes = ExportToCsv(target, snapshots);
                var csvEntry = archive.CreateEntry($"{targetName}_metrics.csv");
                using (var csvStream = csvEntry.Open())
                {
                    csvStream.Write(csvBytes, 0, csvBytes.Length);
                }
            }
        }

        return outputStream.ToArray();
    }

    public static byte[] ExportToCsv(RuntimeObservationTarget target, IReadOnlyList<RecordedSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0)
        {
            return Array.Empty<byte>();
        }

        List<Dictionary<string, string>> flatRows = new(snapshots.Count);
        HashSet<string> uniqueHeaders = [];

        foreach (var s in snapshots)
        {
            var data = ReportJsonParser.Parse(s.ObservationData);
            var flatRow = new Dictionary<string, string>(StringComparer.Ordinal);
            Flatten("", data, flatRow);
            flatRows.Add(flatRow);

            foreach (var key in flatRow.Keys)
            {
                uniqueHeaders.Add(key);
            }
        }

        var headerList = uniqueHeaders.OrderBy(h => h).ToList();

        var sb = new StringBuilder();
        // Header
        sb.Append("Timestamp");
        foreach (var header in headerList)
        {
            sb.Append(',');
            sb.Append(EscapeCsv(header));
        }
        sb.AppendLine();

        // Data Rows
        for (int i = 0; i < snapshots.Count; i++)
        {
            var s = snapshots[i];
            var flatRow = flatRows[i];

            sb.Append(s.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            foreach (var header in headerList)
            {
                sb.Append(',');
                if (flatRow.TryGetValue(header, out string? val))
                {
                    sb.Append(EscapeCsv(val));
                }
            }
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void Flatten(string prefix, IReadOnlyDictionary<string, object?> dictionary, Dictionary<string, string> flatData)
    {
        foreach (var kvp in dictionary)
        {
            string key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
            if (kvp.Value is IReadOnlyDictionary<string, object?> nestedDict)
            {
                Flatten(key, nestedDict, flatData);
            }
            else if (kvp.Value is System.Collections.IDictionary dict)
            {
                var genericDict = new Dictionary<string, object?>();
                foreach (var k in dict.Keys)
                {
                    if (k != null)
                    {
                        genericDict[k.ToString()!] = dict[k];
                    }
                }
                Flatten(key, genericDict, flatData);
            }
            else if (kvp.Value is System.Collections.IEnumerable list && kvp.Value is not string)
            {
                flatData[key] = JsonSerializer.Serialize(kvp.Value);
            }
            else
            {
                flatData[key] = kvp.Value?.ToString() ?? "";
            }
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.Contains(',', StringComparison.Ordinal) || 
            value.Contains('"', StringComparison.Ordinal) || 
            value.Contains('\n', StringComparison.Ordinal) || 
            value.Contains('\r', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
