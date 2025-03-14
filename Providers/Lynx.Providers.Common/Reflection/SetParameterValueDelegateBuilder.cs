using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Lynx.Providers.Common.Models;

namespace Lynx.Providers.Common.Reflection;

/// <summary>
/// Builds expressions for adding parameters to a command.
/// </summary>
[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
internal static class SetParameterValueDelegateBuilder<TCommand, TMapper, TEntity>
    where TCommand : DbCommand 
    where TEntity : class
    where TMapper : IProviderDelegateBuilder
{
    /// <summary>
    /// Parameter expression for the command.
    /// </summary>
    private static readonly ParameterExpression Command = Expression.Parameter(typeof(TCommand), "command");

    public static Action<TCommand, TEntity> Build(EntityInfo entity)
    {
        if (entity.Type.ClrType != typeof(TEntity))
            throw new ArgumentException("Entity type mismatch", nameof(entity));

        var entityValue = Expression.Parameter(typeof(TEntity), typeof(TEntity).Name.ToLowerInvariant());

        var block = Expression.Block(SetParameters(entity, entityValue));
        return Expression.Lambda<Action<TCommand, TEntity>>(block, Command, entityValue).Compile();
    }

    private static IEnumerable<Expression> SetParameters(IStructureEntity entity, Expression? entityValue)
    {
        var keys = (entity as RootEntityInfo)?.Keys ?? [];

        var setScalars =
            from scalar in keys.Concat(entity.ScalarProps)
            select SetScalar(scalar, entityValue);

        var setComplex =
            from complex in entity.ComplexProps
            from e in SetStructure(complex, entityValue)
            select e;

        var setOwned =
            from owned in (entity as EntityInfo)?.Owned ?? []
            from e in SetOwned(owned, entityValue)
            select e;

        return setScalars.Concat(setComplex).Concat(setOwned);
    }

    /// <summary>
    /// Builds expression to set a scalar property parameter value
    /// </summary>
    private static Expression SetScalar(
        ScalarEntityPropertyInfo property,
        Expression? entity)
    {
        Debug.Assert(property.ColumnIndex >= 0, $"Property {property.Name} column index is not set.");

        if (entity == null)
        {
            //We are inside a null check, set to null
            return SetParameterValue(property.ColumnIndex, null);
        }

        Expression value = Expression.Property(entity, property.PropertyInfo);

        var converter = property.TypeMapping.Converter;
        if (converter != null)
            value = ConverterHelpers.InvokeConverter(converter, value);

        return SetParameterValue(property.ColumnIndex, value);
    }

    /// <summary>
    /// Builds expressions to set all parameters for a structure property.
    /// </summary>
    private static IEnumerable<Expression> SetStructure(
        IStructurePropertyInfo property,
        Expression? entity)
    {
        //Expression to set all properties to null
        var setNull = SetParameters(property, null);
        if (entity == null)
            //We are inside a null check, return with complex set to null
            return setNull;

        var value = Expression.Property(entity, property.PropertyInfo);

        //Expression that sets all properties to their values
        var setNotNull = SetParameters(property, value);

        var ifNotNull = ExpressionHelpers.GetIfNotNull(value);
        if (ifNotNull == null)
            //Complex property is not nullable, ignore null check
            return setNotNull;

        //Nullable check
        //If value is null, then set all parameters to null
        //Otherwise set all parameters to their values
        var nullCheck = Expression.IfThenElse(
            ifNotNull,
            Expression.Block(setNotNull),
            Expression.Block(setNull));
        return [nullCheck];
    }

    /// <summary>
    /// Builds expressions to set all parameters for an owned property.
    /// </summary>
    private static IEnumerable<Expression> SetOwned(
        OwnedEntityInfo owned,
        Expression? entity)
    {
        if (owned is not JsonOwnedEntityInfo json)
            //Owned entity is not mapped to JSON, treat as structure
            return SetStructure(owned, entity);

        //Owned entity is mapped to JSON, handle as a scalar
        Debug.Assert(json.ColumnIndex >= 0, $"Json owned property {json.Name} column index is not set.");

        if (entity == null)
            //Inside a null check, set to null
            return [SetParameterValue(json.ColumnIndex, null)];

        //Get the value of the property and convert it to JSON
        Expression value = Expression.Property(entity, json.PropertyInfo);
        value = TMapper.SerializeJson(value);
        return [SetParameterValue(json.ColumnIndex, value)];
    }

    /// <summary>
    /// Builds expression to set parameter value at the given column index.
    /// </summary>
    /// <param name="columnIndex">Parameter index</param>
    /// <param name="value">Value to set parameter to, or null to set to DBNull</param>
    private static Expression SetParameterValue(int columnIndex, Expression? value)
    {
        Debug.Assert(columnIndex >= 0);
        var parameterValue = Expression.Property(
            Expression.Call(
                Expression.Property(Command, ReflectionItems.CommandParametersProperty),
                ReflectionItems.ParameterGetItemMethod,
                Expression.Constant(columnIndex)),
            ReflectionItems.ParameterValueProperty);

        if (value == null)
            //Input is null, set to DBNull
            return Expression.Assign(parameterValue, ReflectionItems.DBNullValue);

        Debug.Assert(value.Type != typeof(void));

        var isNullable = ExpressionHelpers.IsNullable(parameterValue);

        if (value.Type != typeof(object))
            value = Expression.Convert(value, typeof(object));

        if (isNullable)
            //Parameter is nullable, coalesce to DBNull if null
            value = Expression.Coalesce(value, ReflectionItems.DBNullValue);

        return Expression.Assign(parameterValue, value);
    }
}