using System.Data;
using System.Data.Common;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// Reads tables to backup from a PostgreSQL database
/// </summary>
internal static class PgTableReader
{

    /// <summary>
    /// Gets tables sorted in order that they can be restored
    /// </summary>
    /// <param name="connection"></param>
    public static List<PgTableGraph> GetTables(NpgsqlConnection connection)
    {
        var tables = Read(connection, GetTablesSql, r => new PgTableGraph(r));
        var dependencies = Read(connection, GetDependenciesSql, r => new Dependency(r));
        var columns = Read(connection, GetColumnsSql, r => new Column(r));
        return Merge(tables, dependencies, columns);
    }

    /// <summary>
    /// Gets tables sorted in order that they can be restored
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<List<PgTableGraph>> GetTablesAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var tables = await ReadAsync(connection, GetTablesSql, r => new PgTableGraph(r), ct);
        var dependencies = await ReadAsync(connection, GetDependenciesSql, r => new Dependency(r), ct);
        var columns = await ReadAsync(connection, GetColumnsSql, r => new Column(r), ct);

        return Merge(tables, dependencies, columns);
    }

    /// <summary>
    /// Merges dependencies and columns into tables, and sorts tables in order that they can be restored
    /// </summary>
    private static List<PgTableGraph> Merge(List<PgTableGraph> tables, List<Dependency> dependencies, List<Column> columns)
    {
        foreach (var t in tables)
        {
            t.Dependencies.AddRange(
                from d in dependencies
                where d.Schema == t.Schema && d.Table == t.Table
                select tables.Single(x => x.Schema == d.ForeignSchema && x.Table == d.ForeignTable));
            t.Columns.AddRange(
                from c in columns
                where c.Schema == t.Schema && c.Table == t.Table
                orderby c.Position
                select c.Name);
        }

        var list = tables.OrderBy(t => t.Schema).ThenBy(t => t.Table).ToList();
        var sorted = new List<PgTableGraph>();
        while (list.Count > 0)
        {
            var canAdd = list.Where(t => t.Dependencies.All(d => sorted.Contains(d) || d == t)).ToList();
            if (canAdd.Count == 0)
                throw new InvalidOperationException("Circular dependency detected");
            sorted.AddRange(canAdd);
            list.RemoveAll(sorted.Contains);
        }
        return sorted;
    }

    private record Dependency(string Schema, string Table, string ForeignTable, string ForeignSchema)
    {
        public Dependency(DbDataReader reader)
            : this(reader.GetString(nameof(Schema)),
                reader.GetString(nameof(Table)),
                reader.GetString(nameof(ForeignTable)),
                reader.GetString(nameof(ForeignSchema))) {}
    }

    private record Column(string Schema, string Table, string Name, int Position)
    {
        public Column(DbDataReader reader)
            : this(reader.GetString(nameof(Schema)),
                reader.GetString(nameof(Table)),
                reader.GetString(nameof(Name)),
                reader.GetInt32(nameof(Position))) {}
    }

    #region SQL


    private const string GetTablesSql = """
                                        SELECT n.nspname AS "Schema",
                                               c.relname AS "Table"
                                        FROM  pg_catalog.pg_class c
                                        JOIN  pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                                        WHERE c.relkind = 'r' -- ordinary table
                                            AND n.nspname !~ ALL ('{^pg_,^information_schema$}') -- exclude system schemas
                                        ORDER BY "Schema", "Table";
                                        """;

    private const string GetDependenciesSql = """
                                            SELECT DISTINCT
                                                tc.table_schema as "Schema",
                                                tc.table_name as "Table",
                                                ccu.table_schema AS "ForeignSchema",
                                                ccu.table_name AS "ForeignTable"
                                            FROM information_schema.table_constraints AS tc
                                                JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
                                                JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name
                                            WHERE tc.constraint_type = 'FOREIGN KEY'
                                            """;

    private const string GetColumnsSql = """
                                         select table_schema as "Schema", 
                                                table_name as "Table", 
                                                column_name as "Name",
                                                ordinal_position as "Position"
                                         from information_schema.columns
                                         where is_generated='NEVER' -- exclude generated columns
                                           AND table_schema !~ ALL ('{^pg_,^information_schema$}') -- exclude system schemas
                                         order by table_schema, table_name, ordinal_position;
                                         """;

    #endregion

    #region Read

    private static List<T> Read<T>(NpgsqlConnection connection, string sql, Func<NpgsqlDataReader, T> selector)
    {
        if (connection.State != ConnectionState.Open)
            connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();
        var result = new List<T>();
        while (reader.Read())
            result.Add(selector(reader));
        return result;
    }

    private static async Task<List<T>> ReadAsync<T>(
        NpgsqlConnection connection,
        string sql,
        Func<NpgsqlDataReader, T> selector,
        CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<T>();
        while (await reader.ReadAsync(ct))
            result.Add(selector(reader));
        return result;
    }

    #endregion
}