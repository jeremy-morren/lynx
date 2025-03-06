using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Models;
using Lynx.Provider.Common.Reflection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;

namespace Lynx.Provider.Npgsql;

[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
internal abstract class NpgsqlProviderDelegateBuilder : IProviderDelegateBuilder
{
    private static NpgsqlDbType? GetDbType(ScalarEntityPropertyInfo property)
    {
        if (property.TypeMapping is NpgsqlArrayTypeMapping array)
        {
            //Get the npgsqlDbType from the array type mapping
            //For some reason, NpgsqlArrayTypeMapping does not implement INpgsqlTypeMapping
            return ((dynamic)array).NpgsqlDbType;
        }

        if (property.TypeMapping.ElementTypeMapping is INpgsqlTypeMapping element)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            return NpgsqlDbType.Array | element.NpgsqlDbType;
        }

        return (property.TypeMapping as INpgsqlTypeMapping)?.NpgsqlDbType;
        
    }
    
    public static Expression? SetupParameterDbType(ParameterExpression parameter, ScalarEntityPropertyInfo property)
    {
        var npgsqlDbType = GetDbType(property);
        if (npgsqlDbType == null)
            return null;
        
        //Set the NpgsqlDbType to the property's NpgsqlDbType
        return Expression.Assign(
            Expression.Property(parameter, NpgsqlDbTypeProperty),
            Expression.Constant(npgsqlDbType.Value, typeof(NpgsqlDbType)));
    }
    
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