using System.Text.Json.Nodes;

namespace TICSaveEditor.Core.GameData.Nex;

/// <summary>
/// Shared parser for the DB Browser for SQLite "Export as JSON" shape used by every
/// Nex catalog reader: <c>{"type":"table", "columns":[{"name":"...","type":"..."}, ...], "rows":[[...], ...]}</c>.
/// </summary>
internal static class NexCatalogParser
{
    public static (JsonArray Rows, IReadOnlyDictionary<string, int> ColumnIndices) Parse(
        Stream jsonStream, string tableLabel)
    {
        if (jsonStream is null) throw new ArgumentNullException(nameof(jsonStream));

        var root = JsonNode.Parse(jsonStream)
            ?? throw new InvalidDataException($"{tableLabel}.json is empty or not valid JSON.");

        var columns = root["columns"]?.AsArray()
            ?? throw new InvalidDataException($"{tableLabel}.json is missing the \"columns\" array.");
        var rows = root["rows"]?.AsArray()
            ?? throw new InvalidDataException($"{tableLabel}.json is missing the \"rows\" array.");

        var nameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < columns.Count; i++)
        {
            var name = columns[i]?["name"]?.GetValue<string>()
                ?? throw new InvalidDataException(
                    $"{tableLabel}.json columns[{i}] is missing its \"name\" property.");
            nameToIndex[name] = i;
        }
        return (rows, nameToIndex);
    }

    public static int RequireColumn(
        IReadOnlyDictionary<string, int> columnIndices,
        string columnName,
        string tableLabel)
    {
        if (!columnIndices.TryGetValue(columnName, out var idx))
            throw new InvalidDataException(
                $"{tableLabel}.json is missing required column '{columnName}'. " +
                $"Found columns: [{string.Join(", ", columnIndices.Keys)}].");
        return idx;
    }

    public static int ReadInt(JsonArray row, int idx, string columnName, int rowNum, string tableLabel)
    {
        var node = row[idx]
            ?? throw new InvalidDataException(
                $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') is null; " +
                $"required as integer.");
        try
        {
            return node.GetValue<int>();
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw new InvalidDataException(
                $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') is not an integer: " +
                $"{node.ToJsonString()}.");
        }
    }

    public static int ReadIntOrZero(JsonArray row, int idx)
    {
        var node = row[idx];
        if (node is null) return 0;
        try { return node.GetValue<int>(); }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException) { return 0; }
    }

    public static string ReadStringOrEmpty(JsonArray row, int idx)
    {
        var node = row[idx];
        if (node is null) return string.Empty;
        try
        {
            return node.GetValue<string>() ?? string.Empty;
        }
        catch (InvalidOperationException)
        {
            return node.ToJsonString();
        }
    }

    public static bool ReadBool(JsonArray row, int idx)
    {
        // SQLite has no bool type; INTEGER 0/1 is the convention.
        var node = row[idx];
        if (node is null) return false;
        try { return node.GetValue<int>() != 0; }
        catch { return false; }
    }

    public static byte ReadByteOrZero(JsonArray row, int idx, string columnName, int rowNum, string tableLabel)
    {
        var node = row[idx];
        if (node is null) return 0;
        try
        {
            int v = node.GetValue<int>();
            if (v < 0 || v > byte.MaxValue)
                throw new InvalidDataException(
                    $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') value {v} " +
                    $"is out of byte range [0, 255].");
            return (byte)v;
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw new InvalidDataException(
                $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') is not an integer: " +
                $"{node.ToJsonString()}.");
        }
    }

    public static ushort ReadUShort(JsonArray row, int idx, string columnName, int rowNum, string tableLabel)
    {
        var node = row[idx]
            ?? throw new InvalidDataException(
                $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') is null; " +
                $"required as ushort.");
        int v;
        try { v = node.GetValue<int>(); }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw new InvalidDataException(
                $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') is not an integer: " +
                $"{node.ToJsonString()}.");
        }
        if (v < 0 || v > ushort.MaxValue)
            throw new InvalidDataException(
                $"{tableLabel}.json rows[{rowNum}][{idx}] (column '{columnName}') value {v} " +
                $"is out of ushort range [0, 65535].");
        return (ushort)v;
    }
}
