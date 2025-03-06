using System.Linq.Expressions;
using System.Reflection;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Reflection;
using Npgsql;
using NpgsqlTypes;

namespace Lynx.Provider.Npgsql;

internal abstract class NpgsqlDbJsonMapper : IDbJsonMapper
{
    //See https://www.npgsql.org/doc/types/json.html?tabs=datasource#poco-mapping

    public static Expression SetupJsonParameter(Expression parameter)
    {
        //Set the NpgsqlDbType to Jsonb
        return Expression.Assign(
            Expression.Property(parameter, NpgsqlDbTypeProperty),
            Expression.Constant(NpgsqlDbType.Jsonb, typeof(NpgsqlDbType)));
    }

    public static Expression SerializeJson(Expression value)
    {
        //We use dynamic POCO mapping, so we don't need to serialize the object
        return value;
    }

    private static readonly PropertyInfo NpgsqlDbTypeProperty =
        typeof(NpgsqlParameter).GetProperty(nameof(NpgsqlParameter.NpgsqlDbType), ReflectionItems.InstanceFlags)!;
}