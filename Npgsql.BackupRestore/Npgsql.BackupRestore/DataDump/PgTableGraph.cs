using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// A postgres table graph (i.e. a table with its dependencies)
/// </summary>
internal class PgTableGraph : IPgTable
{
    public PgTableGraph(DbDataReader reader)
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
    public List<PgTableGraph> Dependencies { get; } = [];

    public override string ToString() => $"{Schema}.{Table}";

    IReadOnlyList<string> IPgTable.Columns => Columns.AsReadOnly();

    public string ToJson()
    {
        var table = new PgTable()
        {
            Schema = Schema,
            Table = Table,
            Columns = Columns
        };
        return JsonSerializer.Serialize(table);
    }

    public string GetCopyStdOutCommand() => $"COPY \"{Schema}\".\"{Table}\" (\"{string.Join("\", \"", Columns)}\") TO STDOUT (FORMAT BINARY)";

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