using System.Text.Json;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// A postgres table
/// </summary>
internal class PgTable : IPgTable
{
    /// <summary>Table schema</summary>
    public required string Schema { get; init; }

    /// <summary>Table name</summary>
    public required string Table { get; init; }

    /// <summary>Table columns</summary>
    public required IReadOnlyList<string> Columns { get; init; }

    public override string ToString() => $"{Schema}.{Table}";

    public string GetCopyStdInCommand() => $"COPY \"{Schema}\".\"{Table}\" (\"{string.Join("\", \"", Columns)}\") FROM STDIN (FORMAT BINARY)";

    public static PgTable? FromJson(string? json) => json != null ? JsonSerializer.Deserialize<PgTable>(json) : null;
}