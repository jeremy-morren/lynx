using System.Text;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Common.Models;

namespace Lynx.Provider.Npgsql;

internal static class NpgsqlCommandGenerator
{
    /// <summary>
    /// Generates copy from STDIN command for a table
    /// </summary>
    /// <param name="entity">Entity info</param>
    public static string GenerateBinaryCopyCommand(RootEntityInfo entity)
    {
        var sb = new StringBuilder();
        sb.Append("COPY ");
        if (entity.Schema != null)
            sb.Append($"\"{entity.Schema}\".");
        sb.Append($"\"{entity.TableName}\" (");
        foreach (var column in entity.Keys.Concat(entity.GetAllScalarColumns()))
            sb.Append($"\"{column.ColumnName.SqlColumnName}\", ");

        sb.Length -= 2; // Remove trailing comma
        sb.Append(") FROM STDIN (FORMAT BINARY)");
        return sb.ToString();
    }
}