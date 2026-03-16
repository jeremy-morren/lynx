using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags

namespace Lynx.Provider.SqlServer;

[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
internal abstract class SqlServerProviderDelegateBuilder : IProviderDelegateBuilder
{
    #region DB Type

    /// <summary>
    /// Maps <see cref="DbType"/> to <see cref="SqlDbType"/>. See https://www.SqlServer.org/doc/types/basic.html#write-mappings
    /// </summary>
    private static readonly Dictionary<DbType, SqlDbType> TypeMappings = new(capacity: 19)
    {
        { DbType.Boolean, SqlDbType.Bit },
        { DbType.Int16, SqlDbType.SmallInt },
        { DbType.Int32, SqlDbType.Int },
        { DbType.Int64, SqlDbType.BigInt },
        { DbType.Single, SqlDbType.Real },
        { DbType.Double, SqlDbType.Float },
        { DbType.Decimal, SqlDbType.Decimal },
        { DbType.VarNumeric, SqlDbType.Decimal },
        { DbType.Currency, SqlDbType.Money },

        { DbType.String, SqlDbType.NVarChar },
        { DbType.StringFixedLength, SqlDbType.NChar },
        { DbType.AnsiString, SqlDbType.VarChar },
        { DbType.AnsiStringFixedLength, SqlDbType.Char },

        { DbType.Binary, SqlDbType.VarBinary },

        { DbType.DateTime, SqlDbType.DateTime },
        { DbType.DateTimeOffset, SqlDbType.DateTimeOffset },
        { DbType.DateTime2, SqlDbType.DateTime2 },
        { DbType.Date, SqlDbType.Date },
        { DbType.Time, SqlDbType.Time },
    };

    /// <summary>
    /// Gets the <see cref="SqlDbType"/> for a <see cref="DbType"/>
    /// </summary>
    public static SqlDbType GetDbType(DbType dbType) =>
        TypeMappings.TryGetValue(dbType, out var sqlDbtype)
            ? sqlDbtype
            : throw new InvalidOperationException($"Could not determine SqlDbType for DbType {dbType}");

    /// <summary>
    /// Gets the <see cref="SqlDbType"/> for a <see cref="RelationalTypeMapping"/>
    /// </summary>
    public static SqlDbType? GetSqlDbType(RelationalTypeMapping mapping) =>
        mapping switch
        {
            SqlServerByteArrayTypeMapping => SqlDbType.VarBinary,
            _ => mapping.DbType != null ? GetDbType(mapping.DbType.Value) : null
        };

    public static Expression? SetupParameterDbType(ParameterExpression parameter, ScalarEntityPropertyInfo property)
    {
        var sqlDbType = GetSqlDbType(property.TypeMapping);
        if (sqlDbType == null)
            return null;

        //Set the SqlDbType to the property's SqlDbType
        return Expression.Assign(
            Expression.Property(parameter, SqlDbTypeProperty),
            Expression.Constant(sqlDbType.Value, typeof(SqlDbType)));
    }

    #endregion

    #region JSON

    //See https://www.SqlServer.org/doc/types/json.html?tabs=datasource#poco-mapping

    public static Expression SetupJsonParameter(Expression parameter)
    {
        //Set the SqlDbType to Text
        return Expression.Assign(
            Expression.Property(parameter, SqlDbTypeProperty),
            Expression.Constant(SqlDbType.Text, typeof(SqlDbType)));
    }

    private static readonly PropertyInfo SqlDbTypeProperty =
        typeof(SqlParameter).GetProperty(nameof(SqlParameter.SqlDbType), ReflectionItems.InstanceFlags)!;

    #endregion
}