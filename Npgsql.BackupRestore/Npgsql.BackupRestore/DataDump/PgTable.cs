using System.Data;
using System.Data.Common;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// A postgres table
/// </summary>
internal class PgTable
{
    public PgTable(DbDataReader reader)
    {
        Schema = reader.GetString("Schema");
        Table = reader.GetString("Table");
    }

    /// <summary>Table schema</summary>
    public string Schema { get; }

    /// <summary>Table name</summary>
    public string Table { get; }

    /// <summary>Table columns</summary>
    public List<string> Columns { get; } = [];

    /// <summary>Table dependencies (i.e. tables that this table has foreign keys to)</summary>
    public List<PgTable> Dependencies { get; } = [];

    public override string ToString() => $"{Schema}.{Table}";

    public string GetCopyStdOutCommand() => $"COPY \"{Schema}\".\"{Table}\" (\"{string.Join("\", \"", Columns)}\") TO STDOUT (FORMAT BINARY)";

    public string GetCopyStdInCommand() => $"COPY \"{Schema}\".\"{Table}\" (\"{string.Join("\", \"", Columns)}\") FROM STDIN (FORMAT BINARY)";

    public async Task<bool> IsEmptyAsync(NpgsqlCommand command, CancellationToken ct)
    {
        command.CommandText = GetCountQuery();
        var count = (long)(await command.ExecuteScalarAsync(ct))!;
        return count == 0;
    }

    public bool IsEmpty(NpgsqlCommand command)
    {
        command.CommandText = GetCountQuery();
        var count = (long)command.ExecuteScalar()!;
        return count == 0;
    }

    private string GetCountQuery() => $"SELECT COUNT(1) FROM \"{Schema}\".\"{Table}\"";
}