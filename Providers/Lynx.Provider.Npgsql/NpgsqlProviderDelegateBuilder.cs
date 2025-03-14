using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;

// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags

namespace Lynx.Provider.Npgsql;

[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
internal abstract class NpgsqlProviderDelegateBuilder : IProviderDelegateBuilder
{
    #region DB Type

    /// <summary>
    /// Maps <see cref="DbType"/> to <see cref="NpgsqlDbType"/>. See https://www.npgsql.org/doc/types/basic.html#write-mappings
    /// </summary>
    private static readonly Dictionary<DbType, NpgsqlDbType> TypeMappings = new(capacity: 19)
    {
        { DbType.Boolean, NpgsqlDbType.Boolean },
        { DbType.Int16, NpgsqlDbType.Smallint },
        { DbType.Int32, NpgsqlDbType.Integer },
        { DbType.Int64, NpgsqlDbType.Bigint },
        { DbType.Single, NpgsqlDbType.Real },
        { DbType.Double, NpgsqlDbType.Double },
        { DbType.Decimal, NpgsqlDbType.Numeric },
        { DbType.VarNumeric, NpgsqlDbType.Numeric },
        { DbType.Currency, NpgsqlDbType.Money },

        { DbType.String, NpgsqlDbType.Text },
        { DbType.StringFixedLength, NpgsqlDbType.Text },
        { DbType.AnsiString, NpgsqlDbType.Text },
        { DbType.AnsiStringFixedLength, NpgsqlDbType.Text },

        { DbType.Binary, NpgsqlDbType.Bytea },

        { DbType.DateTime, NpgsqlDbType.TimestampTz },
        { DbType.DateTimeOffset, NpgsqlDbType.TimestampTz },
        { DbType.DateTime2, NpgsqlDbType.Timestamp },
        { DbType.Date, NpgsqlDbType.Date },
        { DbType.Time, NpgsqlDbType.Time },
    };

    /// <summary>
    /// Gets the <see cref="NpgsqlDbType"/> for a <see cref="DbType"/>
    /// </summary>
    public static NpgsqlDbType GetDbType(DbType dbType) =>
        TypeMappings.TryGetValue(dbType, out var npgsqlDbType)
            ? npgsqlDbType
            : throw new InvalidOperationException($"Could not determine NpgsqlDbType for DbType {dbType}");

    /// <summary>
    /// Gets the <see cref="NpgsqlDbType"/> for a <see cref="RelationalTypeMapping"/>
    /// </summary>
    public static NpgsqlDbType? GetNpgsqlDbType(RelationalTypeMapping mapping) =>
        mapping switch
        {
            NpgsqlArrayTypeMapping array => ((dynamic)array).NpgsqlDbType,
            INpgsqlTypeMapping npgsqlType => npgsqlType.NpgsqlDbType,
            _ => mapping.DbType != null ? GetDbType(mapping.DbType.Value) : null
        };

    public static Expression? SetupParameterDbType(ParameterExpression parameter, ScalarEntityPropertyInfo property)
    {
        var npgsqlDbType = GetNpgsqlDbType(property.TypeMapping);
        if (npgsqlDbType == null)
            return null;
        
        //Set the NpgsqlDbType to the property's NpgsqlDbType
        return Expression.Assign(
            Expression.Property(parameter, NpgsqlDbTypeProperty),
            Expression.Constant(npgsqlDbType.Value, typeof(NpgsqlDbType)));
    }

    #endregion

    #region JSON
    
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

    #endregion
}