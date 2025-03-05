using System.Linq.Expressions;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Models;
using Microsoft.Data.Sqlite;

namespace Lynx.Provider.Sqlite;

internal class SqliteDbTypeMapper : IDbTypeMapper<SqliteCommand>
{
    public static Expression SetDbType(ParameterExpression command, IEntityPropertyInfo property)
    {
        var cmd = new SqliteCommand();
        var parameter = cmd.CreateParameter();
        throw new NotImplementedException();
    }
}