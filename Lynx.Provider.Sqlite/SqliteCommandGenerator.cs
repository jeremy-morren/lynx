using System.Text;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Models;

namespace Lynx.Provider.Sqlite;

internal static class SqliteCommandGenerator
{
    #region Insert

    /// <summary>
    /// Insert command including primary key.
    /// </summary>
    public static string GetInsertWithKeyCommand(RootEntityInfo entity)
    {
        var properties = entity.Keys.Concat(entity.GetAllProperties());
        return GetInsertCommand(entity, properties).ToString();
    }

    /// <summary>
    /// Insert command without primary key.
    /// </summary>
    public static string GetInsertIdentityCommand(RootEntityInfo entity)
    {
        if (entity.Keys.Count != 1)
            throw new NotImplementedException("Cannot insert identity without exactly one key");
        var sb = GetInsertCommand(entity, entity.GetAllProperties());
        var identity = entity.Keys[0].ColumnName.SqlColumnName;
        sb.Append($" RETURNING \"{identity}\"");
        return sb.ToString();
    }

    /// <summary>
    /// Insert command including primary key.
    /// </summary>
    private static StringBuilder GetInsertCommand(RootEntityInfo entity, IEnumerable<EntityPropertyInfo> properties)
    {
        var list = properties.ToList();
        if (list.Count == 0)
            throw new NotImplementedException("No properties selected");

        var sb = new StringBuilder();
        sb.Append($"INSERT INTO \"{entity.TableName}\" (");
        foreach (var p in list)
        {
            sb.Append($"\"{p.ColumnName.SqlColumnName}\", ");
        }
        sb.Length -= 2; // Remove trailing comma
        sb.Append(") VALUES (");
        foreach (var p in list)
        {
            sb.Append($"@{p.ColumnName.SqlColumnName}, ");
        }
        sb.Length -= 2; // Remove trailing comma
        sb.Append(')');
        return sb;
    }

    #endregion

    #region Upsert

    public static string GetUpsertCommand(RootEntityInfo entity)
    {
        var properties = entity.GetAllProperties().ToList();
        var sb = GetInsertCommand(entity, entity.Keys.Concat(properties));
        sb.Append(" ON CONFLICT (");
        foreach (var p in entity.Keys)
        {
            sb.Append($"\"{p.ColumnName.SqlColumnName}\", ");
        }
        sb.Length -= 2; // Remove trailing comma
        sb.Append(") DO UPDATE SET ");
        foreach (var p in properties)
            sb.Append($"\"{p.ColumnName.SqlColumnName}\" = @{p.ColumnName.SqlColumnName}, ");
        sb.Length -= 2; // Remove trailing comma
        return sb.ToString();
    }

    #endregion
}