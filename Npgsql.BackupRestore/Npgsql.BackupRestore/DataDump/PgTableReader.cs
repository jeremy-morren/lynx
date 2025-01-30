using System.Data;
using System.Data.Common;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// Reads tables to backup from a PostgreSQL database
/// </summary>
internal static class PgTableReader
{
    /// <summary>
    /// A postgres table
    /// </summary>
    public record PgTable
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


        /// <summary>Table dependencies (i.e. tables that this table has foreign keys to)</summary>
        public List<PgTable> Dependencies { get; } = [];

        public void Deconstruct(out string schema, out string table)
        {
            schema = Schema;
            table = Table;
        }

        public override string ToString() => $"{Schema}.{Table}";
    }

    /// <summary>
    /// Gets tables sorted in order that they can be restored
    /// </summary>
    /// <param name="connection"></param>
    public static List<PgTable> GetTables(NpgsqlConnection connection)
    {
        var tables = Read(connection, GetTablesSql, r => new PgTable(r))
            .ToDictionary(t => (t.Schema, t.Table));

        var dependencies = Read(connection,
            GetDependenciesSql,
            r => new
            {
                Schema = r.GetString("Schema"),
                Table = r.GetString("Table"),
                ForeignSchema = r.GetString("ForeignSchema"),
                ForeignTable = r.GetString("ForeignTable")
            });

        foreach (var d in dependencies)
        {
            var table = tables[(d.Schema, d.Table)];
            var dependency = tables[(d.ForeignSchema, d.ForeignTable)];
            table.Dependencies.Add(dependency);
        }
        return Sort(tables.Values);
    }

    /// <summary>
    /// Gets tables sorted in order that they can be restored
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<List<PgTable>> GetTablesAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var tables = (await ReadAsync(connection, GetTablesSql, r => new PgTable(r), ct))
            .ToDictionary(t => (t.Schema, t.Table));

        var dependencies = await ReadAsync(connection,
            GetDependenciesSql,
            r => new
            {
                Schema = r.GetString("Schema"),
                Table = r.GetString("Table"),
                ForeignSchema = r.GetString("ForeignSchema"),
                ForeignTable = r.GetString("ForeignTable")
            },
            ct);

        foreach (var d in dependencies)
        {
            var table = tables[(d.Schema, d.Table)];
            var dependency = tables[(d.ForeignSchema, d.ForeignTable)];
            table.Dependencies.Add(dependency);
        }
        return Sort(tables.Values);
    }

    /// <summary>
    /// Sorts tables in order that they can be restored
    /// </summary>
    private static List<PgTable> Sort(IEnumerable<PgTable> tables)
    {
        var list = tables.OrderBy(t => t.Schema).ThenBy(t => t.Table).ToList();
        var sorted = new List<PgTable>();
        while (list.Count > 0)
        {
            var canAdd = list.Where(d => d.Dependencies.All(sorted.Contains)).ToList();
            if (canAdd.Count == 0)
                throw new InvalidOperationException("Circular dependency detected");
            sorted.AddRange(canAdd);
            list.RemoveAll(sorted.Contains);
        }
        return sorted;
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