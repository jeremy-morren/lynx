using System.Data;
using System.Text;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Models;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.SqlServer;

/// <summary>
/// Generates SQL Server commands for a root entity.
/// </summary>
internal class SqlServerCommandGenerator
{
    private readonly RootEntityInfo _entity;

    public SqlServerCommandGenerator(RootEntityInfo entity) => _entity = entity;

    /// <summary>
    /// Gets a command to turn identity insert on/off, if necessary
    /// </summary>
    public string? GetSetIdentityInsertCommand(bool on)
    {
        // Necessary to set identity insert only if there is a single key that we can insert identity
        if (!_entity.Keys.All(k => IsGenerated(k.Property.ValueGenerated)))
            return null;

        var sb = new StringBuilder();

        sb.Append("SET IDENTITY_INSERT ");
        AppendQualifiedTableName(sb);
        sb.Append(on ? " ON" : " OFF");
        return sb.ToString();

        static bool IsGenerated(ValueGenerated generated) =>
            generated is ValueGenerated.OnAdd or ValueGenerated.OnAddOrUpdate;
    }

    /// <summary>
    /// Insert command including primary key.
    /// </summary>
    public string GetInsertWithKeyCommand()
    {
        var sb = new StringBuilder();
        WriteInsertCommand(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Insert command including primary key.
    /// </summary>
    private void WriteInsertCommand(StringBuilder sb)
    {
        var properties = _entity.Keys.Concat(_entity.GetAllScalarColumns()).ToList();

        sb.Append("INSERT INTO ");
        AppendQualifiedTableName(sb);
        sb.Append(" (");
        foreach (var p in properties)
            sb.Append($"[{p.ColumnName.SqlColumnName}], ");
        sb.Length -= 2; // Remove trailing comma
        sb.Append(") VALUES (");
        foreach (var p in properties)
            sb.Append($"{p.ColumnName.SqlParamName}, ");
        sb.Length -= 2; // Remove trailing comma
        sb.Append(')');
    }

    public string GetUpsertCommand()
    {
        var properties = _entity.GetAllScalarColumns().ToList();

        var sb = new StringBuilder();

        if (properties.Count > 0)
        {
            // Has property values: Result is UPDATE ; IF @@ROWCOUNT = 0 THEN BEGIN INSERT

            sb.Append("UPDATE ");
            AppendQualifiedTableName(sb);
            sb.AppendLine();

            sb.Append("SET ");
            foreach (var p in properties)
                sb.Append($"[{p.ColumnName.SqlColumnName}] = {p.ColumnName.SqlParamName}, ");
            sb.Length -= 2; // Remove trailing comma
            sb.AppendLine();

            sb.Append("WHERE ");
            AppendKeyPredicate(sb);
            sb.AppendLine(";");

            sb.AppendLine();
            sb.AppendLine("IF @@ROWCOUNT = 0");
        }
        else
        {
            // No properties: Result is IF NOT EXISTS (...) THEN BEGIN INSERT
            sb.Append("IF NOT EXISTS (SELECT 1 FROM ");
            AppendQualifiedTableName(sb);
            sb.Append(" WHERE ");
            AppendKeyPredicate(sb);
            sb.AppendLine(")");
        }

        sb.AppendLine("BEGIN");
        sb.Append("    ");
        WriteInsertCommand(sb);
        sb.AppendLine(";");
        sb.Append("END");

        return sb.ToString();
    }

    private void AppendQualifiedTableName(StringBuilder sb)
    {
        if (_entity.Schema != null)
            sb.Append($"[{_entity.Schema}].");

        sb.Append($"[{_entity.TableName}]");
    }

    private void AppendKeyPredicate(StringBuilder sb)
    {
        foreach (var key in _entity.Keys)
            sb.Append($"[{key.ColumnName.SqlColumnName}] = {key.ColumnName.SqlParamName} AND ");

        sb.Length -= 5; // Remove trailing ' AND '
    }
}