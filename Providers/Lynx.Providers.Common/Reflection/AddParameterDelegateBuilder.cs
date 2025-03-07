using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Providers.Common.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.Providers.Common.Reflection;

/// <summary>
/// Builds expressions for adding all parameters for an entity to a command.
/// </summary>
[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
internal static class AddParameterDelegateBuilder<TCommand, TDelegateBuilder>
    where TCommand : DbCommand
    where TDelegateBuilder : IProviderDelegateBuilder
{
    /// <summary>
    /// <see cref="DbCommand.CreateParameter"/>
    /// </summary>
    private static readonly MethodInfo CreateParameterMethod =
        typeof(TCommand).GetMethod(nameof(DbCommand.CreateParameter), ReflectionItems.InstanceFlags)!;

    /// <summary>
    /// Command action parameter
    /// </summary>
    private static readonly ParameterExpression Command = Expression.Parameter(typeof(TCommand), "command");

    /// <summary>
    /// Db parameter temporary variable
    /// </summary>
    private static readonly ParameterExpression Parameter = Expression.Variable(CreateParameterMethod.ReturnType, "parameter");

    public static Action<TCommand> Build(IStructureEntity entity)
    {
        var add = AddParameters(entity).ToList();
        var block = Expression.Block([Parameter], add);
        return Expression.Lambda<Action<TCommand>>(block, Command).Compile();
    }

    private static IEnumerable<Expression> AddParameters(IStructureEntity entity)
    {
        var keys = (entity as RootEntityInfo)?.Keys ?? [];
        
        var addScalars =
            from scalar in keys.Concat(entity.ScalarProps)
            from e in BuildAddParameter(scalar)
            select e;

        var addComplex =
            from complex in entity.ComplexProps
            from e in AddParameters(complex)
            select e;

        var addOwned =
            from owned in (entity as EntityInfo)?.Owned ?? []
            from e in AddOwned(owned)
            select e;

        return addScalars.Concat(addComplex).Concat(addOwned);
    }

    private static IEnumerable<Expression> AddOwned(OwnedEntityInfo owned)
    {
        if (owned is not JsonOwnedEntityInfo json)
            // Not a JSON column, recurse
            return AddParameters(owned);

        // JSON columns are scalar
        // Create parameter and setup
        return
        [
            //Create parameter and assign to the command
            Expression.Assign(
                Parameter,
                Expression.Call(Command, CreateParameterMethod)),
            //Set parameter name
            Expression.Assign(
                Expression.Property(Parameter, ReflectionItems.ParameterNameProperty),
                Expression.Constant(json.ColumnName.SqlParamName)),
            //Setup JSON mapper
            TDelegateBuilder.SetupJsonParameter(Parameter),
            //Add the parameter to the command
            Expression.Call(
                Expression.Property(Command, ReflectionItems.CommandParametersProperty),
                ReflectionItems.AddParameterMethod,
                Parameter)
        ];

    }

    /// <summary>
    /// Returns an expression that checks if the given expression is not null null, or null if the expression is not nullable.
    /// </summary>
    private static List<Expression> BuildAddParameter(ScalarEntityPropertyInfo property)
    {
        var mapping = property.TypeMapping;
        var result = new List<Expression>()
        {
            //Create parameter and assign to the command
            Expression.Assign(
                Parameter,
                Expression.Call(Command, CreateParameterMethod)),
            //Set parameter name
            Expression.Assign(
                Expression.Property(Parameter, ReflectionItems.ParameterNameProperty),
                Expression.Constant(property.ColumnName.SqlParamName))
        };
        
        //Get provider-specific expression to set DbType
        var setDbType = TDelegateBuilder.SetupParameterDbType(Parameter, property);
        if (setDbType != null)
            result.Add(setDbType);
        
        //Set size, scale, and precision
        var elementTypeMapping = mapping.ElementTypeMapping as RelationalTypeMapping;
        var dbType = mapping.DbType ?? elementTypeMapping?.DbType;
        var size = mapping.Size ?? elementTypeMapping?.Size;
        var scale = mapping.Scale ?? elementTypeMapping?.Scale;
        var precision = mapping.Precision ?? elementTypeMapping?.Precision;
        
        if (dbType != null && setDbType == null)
        {
            //No provider-specific expression to set DbType, set it here
            result.Add(
                Expression.Assign(
                    Expression.Property(Parameter, ReflectionItems.DbParameterDbTypeProperty),
                    Expression.Constant(dbType.Value, typeof(DbType))));
        }

        if (size != null)
        {
            //Set size
            result.Add(
                Expression.Assign(
                    Expression.Property(Parameter, ReflectionItems.DbParameterSizeProperty),
                    Expression.Constant(size.Value)));
        }
        
        
        if (scale != null)
        {
            //Set scale
            result.Add(
                Expression.Assign(
                    Expression.Property(Parameter, ReflectionItems.DbParameterScaleProperty),
                    Expression.Constant(scale.Value)));
        }
        if (precision != null)
        {
            //Set precision
            result.Add(
                Expression.Assign(
                    Expression.Property(Parameter, ReflectionItems.DbParameterPrecisionProperty),
                    Expression.Constant(precision.Value)));
        }

        //Add the parameter to the command
        result.Add(
            Expression.Call(
                Expression.Property(Command, ReflectionItems.CommandParametersProperty),
                ReflectionItems.AddParameterMethod,
                Parameter));

        return result;
    }
}