using System.Text;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Models;

namespace Lynx.Providers.Common;

/// <summary>
/// Generates commands (Postgres dialect, understood by Sqlite)
/// </summary>
internal class CommandGenerator
{
    private readonly RootEntityInfo _entity;

    public CommandGenerator(RootEntityInfo entity) => _entity = entity;

    #region Insert

    /// <summary>
    /// Insert command including primary key.
    /// </summary>
    public string GetInsertWithKeyCommand()
    {
        var properties = _entity.Keys.Concat(_entity.GetAllScalarColumns());
        return GetInsertCommand(properties).ToString();
    }

    /// <summary>
    /// Insert command without primary key.
    /// </summary>
    public string GetInsertIdentityCommand()
    {
        if (_entity.Keys.Count != 1)
            throw new NotImplementedException("Cannot insert identity without exactly one key");
        var sb = GetInsertCommand(_entity.GetAllScalarColumns());
        var identity = _entity.Keys[0].ColumnName.SqlColumnName;
        sb.Append($" RETURNING \"{identity}\"");
        return sb.ToString();
    }

    /// <summary>
    /// Insert command including primary key.
    /// </summary>
    private StringBuilder GetInsertCommand(IEnumerable<IEntityPropertyInfo> properties)
    {
        properties = properties.ToList();

        var sb = new StringBuilder();
        sb.Append("INSERT INTO ");
        if (_entity.Schema != null)
            sb.Append($"\"{_entity.Schema}\".");
        sb.Append($"\"{_entity.TableName}\" (");
        foreach (var p in properties)
            sb.Append($"\"{p.ColumnName.SqlColumnName}\", ");
        sb.Length -= 2; // Remove trailing comma
        sb.Append(") VALUES (");
        foreach (var p in properties)
            sb.Append($"{p.ColumnName.SqlParamName}, ");
        sb.Length -= 2; // Remove trailing comma
        sb.Append(')');
        return sb;
    }

    #endregion

    #region Upsert

    public string GetUpsertCommand()
    {
        var properties = _entity.GetAllScalarColumns().ToList();
        var sb = GetInsertCommand(_entity.Keys.Concat(properties));
        sb.AppendLine();
        sb.Append("ON CONFLICT (");
        foreach (var p in _entity.Keys)
            sb.Append($"\"{p.ColumnName.SqlColumnName}\", ");
        sb.Length -= 2; // Remove trailing comma
        sb.AppendLine(")");
        if (properties.Count == 0)
        {
            // No columns to update
            sb.Append("DO NOTHING");
            return sb.ToString();
        }
        sb.Append("DO UPDATE SET ");
        foreach (var p in properties)
            sb.Append($"\"{p.ColumnName.SqlColumnName}\" = {p.ColumnName.SqlParamName}, ");
        sb.Length -= 2; // Remove trailing comma
        return sb.ToString();
    }

    #endregion
}