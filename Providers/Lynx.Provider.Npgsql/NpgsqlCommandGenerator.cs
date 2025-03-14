using System.Diagnostics;
using System.Text;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Models;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlCommandGenerator : CommandGenerator
{
    private readonly RootEntityInfo _entity;
    public NpgsqlCommandGenerator(RootEntityInfo entity) : base(entity) => _entity = entity;

    /// <summary>
    /// Gets the name of the temporary table for an entity
    /// </summary>
    public string TempTableName => $"{_entity.TableName}_TempInsert";

    /// <summary>
    /// Gets the schema qualified table name
    /// </summary>
    public string QualifiedTableName =>
        _entity.Schema == null ? $"\"{_entity.TableName}\"" : $"\"{_entity.Schema}\".\"{_entity.TableName}\"";

    /// <summary>
    /// Generates <c>COPY FROM STDIN ... (FORMAT BINARY)</c> command for a table
    /// </summary>
    public string GenerateBinaryCopyInsertCommand() =>
        GenerateBinaryCopyCommand(_entity.Schema, _entity.TableName);

    /// <summary>
    /// Generates <c>COPY FROM STDIN tempTable ... (FORMAT BINARY)</c> command for a table
    /// </summary>
    public string GenerateBinaryCopyTempTableInsertCommand() =>
        GenerateBinaryCopyCommand(null, TempTableName);

    private string GenerateBinaryCopyCommand(string? schema, string? table)
    {
        var sb = new StringBuilder();
        sb.Append("COPY ");
        if (schema != null)
            sb.Append($"\"{_entity.Schema}\".");
        sb.Append($"\"{table}\" (");
        foreach (var column in _entity.Keys.Concat(_entity.GetAllScalarColumns()))
            sb.Append($"\"{column.ColumnName.SqlColumnName}\", ");

        sb.Length -= 2; // Remove trailing comma
        sb.AppendLine(")");
        sb.Append("FROM STDIN (FORMAT BINARY)");
        return sb.ToString();
    }

    /// <summary>
    /// Generates command to upsert rows from a temporary table to the main table
    /// </summary>
    /// <returns></returns>
    public string GenerateUpsertTempTableCommand()
    {
        var columns = _entity.Keys.Concat(_entity.GetAllScalarColumns()).ToList();
        Debug.Assert(columns.Count > 0);
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO {QualifiedTableName} (");
        foreach (var column in columns)
            sb.Append($"\"{column.ColumnName.SqlColumnName}\", ");
        sb.Length -= 2; // Remove trailing comma
        sb.AppendLine(")");
        sb.Append("SELECT ");
        foreach (var column in columns)
            sb.Append($"\"{column.ColumnName.SqlColumnName}\", ");
        sb.Length -= 2; // Remove trailing comma
        sb.AppendLine();
        sb.AppendLine($"FROM \"{TempTableName}\"");
        sb.Append("ON CONFLICT (");
        foreach (var column in _entity.Keys)
            sb.Append($"\"{column.ColumnName.SqlColumnName}\", ");
        sb.Length -= 2; // Remove trailing comma
        sb.AppendLine(")");
        if (!_entity.GetAllScalarColumns().Any())
        {
            //No columns to update
            sb.Append("DO NOTHING");
            return sb.ToString();
        }
        sb.Append("DO UPDATE SET ");
        foreach (var column in _entity.GetAllScalarColumns())
            sb.Append($"\"{column.ColumnName.SqlColumnName}\" = excluded.\"{column.ColumnName.SqlColumnName}\", ");
        sb.Length -= 2; // Remove trailing comma
        return sb.ToString();
    }

    /// <summary>
    /// Get the SQL command to create a temporary table for an entity.
    /// </summary>
    /// <remarks>
    /// <c>CREATE TEMPORARY TABLE Table_TempInsert ON COMMIT DROP AS schema.Table WITH NO DATA</c>
    /// </remarks>
    public string GetCreateTempTableCommand() =>
        $"CREATE TEMPORARY TABLE \"{TempTableName}\" ON COMMIT DROP AS TABLE {QualifiedTableName} WITH NO DATA";

    /// <summary>
    /// Get the SQL command to drop a temporary table for an entity.
    /// </summary>
    public string GetDropTempTableCommand() => $"DROP TABLE \"{TempTableName}\"";
}