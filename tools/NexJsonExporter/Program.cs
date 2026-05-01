using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TICSaveEditor.Tools.NexJsonExporter;

// Dev-only helper that exports a single SQLite table to the DB Browser for SQLite
// JSON shape that TICSaveEditor.Core's Nex readers expect verbatim:
//
//   {
//     "type": "table",
//     "database": null,
//     "name": "<Table>-<lang>",
//     "withoutRowId": false,
//     "strict": false,
//     "ddl": "CREATE TABLE ...",
//     "columns": [{"name","type"},...],
//     "rows": [[...],[...]]
//   }
//
// Usage:
//   NexJsonExporter --db <path> --table <SqliteTableName> --out <jsonPath>
//
// NOT part of TICSaveEditor.sln. Never invoked at runtime. Never shipped to
// end users. See memory: decisions_build_pipeline_automation.md.
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var opts = ParseArgs(args);
            Run(opts);
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"NexJsonExporter: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: NexJsonExporter --db <path> --table <SqliteTableName> --out <jsonPath>");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NexJsonExporter: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    private sealed record Options(string Db, string Table, string Out);

    private static Options ParseArgs(string[] args)
    {
        string? db = null, table = null, outPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--db": db = NextValue(args, ref i, "--db"); break;
                case "--table": table = NextValue(args, ref i, "--table"); break;
                case "--out": outPath = NextValue(args, ref i, "--out"); break;
                default: throw new ArgumentException($"unknown argument '{args[i]}'");
            }
        }
        if (db is null) throw new ArgumentException("--db is required");
        if (table is null) throw new ArgumentException("--table is required");
        if (outPath is null) throw new ArgumentException("--out is required");
        return new Options(db, table, outPath);
    }

    private static string NextValue(string[] args, ref int i, string flag)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"{flag} requires a value");
        return args[++i];
    }

    private static void Run(Options opts)
    {
        if (!File.Exists(opts.Db))
            throw new FileNotFoundException($"SQLite database not found: {opts.Db}");

        using var connection = new SqliteConnection($"Data Source={opts.Db};Mode=ReadOnly");
        connection.Open();

        var ddl = QueryTableDdl(connection, opts.Table)
            ?? throw new InvalidOperationException(
                $"Table '{opts.Table}' not found in {opts.Db}. " +
                $"Available tables: {string.Join(", ", QueryTableNames(connection))}");

        var columns = QueryColumns(connection, opts.Table);
        var rows = QueryRows(connection, opts.Table, columns.Count);

        var outDir = Path.GetDirectoryName(opts.Out);
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

        WriteJson(opts.Out, opts.Table, ddl, columns, rows);
        Console.WriteLine($"Wrote {opts.Out}  ({rows.Count} rows × {columns.Count} columns)");
    }

    private static string? QueryTableDdl(SqliteConnection connection, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $name";
        cmd.Parameters.AddWithValue("$name", table);
        var result = cmd.ExecuteScalar();
        return result is string ddl ? ddl : null;
    }

    private static List<string> QueryTableNames(SqliteConnection connection)
    {
        var names = new List<string>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    private sealed record ColumnInfo(string Name, string Type);

    private static List<ColumnInfo> QueryColumns(SqliteConnection connection, string table)
    {
        var columns = new List<ColumnInfo>();
        using var cmd = connection.CreateCommand();
        // PRAGMA table_info returns rows: cid, name, type, notnull, dflt_value, pk
        cmd.CommandText = $"PRAGMA table_info({QuoteIdentifier(table)})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            var type = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            columns.Add(new ColumnInfo(name, type));
        }
        if (columns.Count == 0)
            throw new InvalidOperationException($"Table '{table}' has no columns (or doesn't exist).");
        return columns;
    }

    private static List<List<object?>> QueryRows(SqliteConnection connection, string table, int columnCount)
    {
        var rows = new List<List<object?>>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {QuoteIdentifier(table)}";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new List<object?>(columnCount);
            for (int c = 0; c < columnCount; c++)
            {
                if (reader.IsDBNull(c)) { row.Add(null); continue; }
                var raw = reader.GetValue(c);
                row.Add(raw switch
                {
                    long l => l,
                    int i => (long)i,
                    double d => d,
                    float f => (double)f,
                    decimal m => (double)m,
                    byte[] b => Convert.ToBase64String(b),
                    bool b => b ? 1L : 0L,
                    _ => raw.ToString()
                });
            }
            rows.Add(row);
        }
        return rows;
    }

    private static string QuoteIdentifier(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    private static void WriteJson(
        string outPath,
        string tableName,
        string ddl,
        IReadOnlyList<ColumnInfo> columns,
        IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        // DB Browser uses 4-space indentation. JsonSerializer's WriteIndented uses 2 spaces;
        // pass IndentSize = 4 explicitly. UnsafeRelaxedJsonEscaping leaves non-ASCII
        // unescaped (matches DB Browser's behavior for JA multibyte chars and keeps
        // file size reasonable).
        var jsonOptions = new JsonWriterOptions
        {
            Indented = true,
            IndentCharacter = ' ',
            IndentSize = 4,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, jsonOptions);

        writer.WriteStartObject();
        writer.WriteString("type", "table");
        writer.WriteNull("database");
        writer.WriteString("name", tableName);
        writer.WriteBoolean("withoutRowId", false);
        writer.WriteBoolean("strict", false);
        // DB Browser appends a trailing ';' to the DDL string when exporting; sqlite_master.sql
        // omits it. Mirror DB Browser exactly so regenerated JSON is byte-equivalent to the
        // committed shape (validation-script regression check stays meaningful).
        writer.WriteString("ddl", ddl.EndsWith(';') ? ddl : ddl + ";");

        writer.WritePropertyName("columns");
        writer.WriteStartArray();
        foreach (var col in columns)
        {
            writer.WriteStartObject();
            writer.WriteString("name", col.Name);
            writer.WriteString("type", col.Type);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WritePropertyName("rows");
        writer.WriteStartArray();
        foreach (var row in rows)
        {
            writer.WriteStartArray();
            foreach (var cell in row)
            {
                switch (cell)
                {
                    case null: writer.WriteNullValue(); break;
                    case long l: writer.WriteNumberValue(l); break;
                    case double d: writer.WriteNumberValue(d); break;
                    case string s: writer.WriteStringValue(s); break;
                    default: writer.WriteStringValue(cell.ToString()); break;
                }
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        writer.Flush();

        // Post-process for DB Browser byte-equivalence: escape '/' as '\/' inside string
        // values. System.Text.Json never emits this escape (RFC 8259 says '/' MAY be
        // escaped); DB Browser always does. Both forms parse identically, but matching
        // DB Browser's bytes makes the validation script's regression check meaningful.
        // Walk bytes, escape every '/' that isn't already preceded by a backslash. JSON
        // grammar restricts '/' to inside string values; numbers/keys/syntax tokens
        // don't contain it, so a global byte-level pass is safe.
        ms.Position = 0;
        using var fs = File.Create(outPath);
        const byte Slash = (byte)'/';
        const byte Backslash = (byte)'\\';
        var buffer = ms.ToArray();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == Slash && (i == 0 || buffer[i - 1] != Backslash))
            {
                fs.WriteByte(Backslash);
            }
            fs.WriteByte(buffer[i]);
        }
    }
}
