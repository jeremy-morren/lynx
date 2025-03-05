using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Provider.Common.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Lynx.Provider.Common;

/// <summary>
/// Builds expressions for adding parameters to a command.
/// </summary>
internal static class SetParameterValueDelegateBuilder<TCommand, TEntity> 
    where TCommand : DbCommand 
    where TEntity : class
{
    public static Action<TCommand, TEntity> Build(EntityInfo entity)
    {
        if (entity.Type.ClrType != typeof(TEntity))
            throw new ArgumentException("Entity type mismatch", nameof(entity));

        var command = Expression.Parameter(typeof(TCommand), "command");
        var entityValue = Expression.Parameter(typeof(TEntity), typeof(TEntity).Name.ToLowerInvariant());

        var block = Expression.Block(SetParameters(entity, entityValue, command));

        return Expression.Lambda<Action<TCommand, TEntity>>(block, command, entityValue).Compile();
    }

    private static IEnumerable<Expression> SetParameters(
        IStructureEntity entity, 
        Expression? entityValue,
        ParameterExpression command)
    {
        var setScalars =
            from scalar in entity.ScalarProps
            select BuildSetParameter(scalar, entityValue, command);

        var setComplexNull =
            from complex in entity.ComplexProps
            from e in SetParameters(complex, null, command)
            select e;

        if (entityValue == null)
            //We are inside a null check, return with complex set to null
            return setScalars.Concat(setComplexNull);

        var setComplexNotNull =
            from complex in entity.ComplexProps
            let property = Expression.Property(entityValue, complex.PropertyInfo)
            from e in SetParameters(complex, property, command)
            select e;

        var notNull = GetNotNull(entityValue);
        if (notNull == null)
            // Entity is not nullable, no need to check for null
            return setScalars.Concat(setComplexNotNull);
        
        // Entity is nullable, use an if-else block to set parameters
        var nullCheck = Expression.IfThenElse(
            notNull,
            Expression.Block(setComplexNotNull),
            Expression.Block(setComplexNull));

        return setScalars.Append(nullCheck);
    }

    private static Expression BuildSetParameter(
        ScalarEntityPropertyInfo property,
        Expression? entity,
        ParameterExpression command)
    {
        Debug.Assert(property.ColumnIndex >= 0);
        var parameter = Expression.ArrayIndex(
            Expression.Property(command, ReflectionItems.CommandParametersProperty),
            Expression.Constant(property.ColumnIndex));

        //If entity is null, we are inside a null check
        Expression value = entity != null
            ? Expression.Convert(
                Expression.Property(entity, property.PropertyInfo),
                typeof(object))
            : Expression.Constant(null, typeof(object));

        return Expression.Assign(parameter, value);
    }

    /// <summary>
    /// Returns an expression that checks if the given expression is not null null, or null if the expression is not nullable.
    /// </summary>
    private static Expression? GetNotNull(Expression expression)
    {
        if (expression.Type.IsValueType)
        {
            if (Nullable.GetUnderlyingType(expression.Type) == null)
                return null; // Value type not nullable, no need to check for null

            // Nullable value type
            // return expression.HasValue;
            return Expression.Property(expression, nameof(Nullable<int>.HasValue));
        }
        var nullValue = Expression.Constant(null, expression.Type);
        return Expression.ReferenceNotEqual(expression, nullValue);
    }

}