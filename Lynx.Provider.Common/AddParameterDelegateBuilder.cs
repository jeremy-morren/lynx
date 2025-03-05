using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Provider.Common.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Lynx.Provider.Common;

/// <summary>
/// Builds expressions for adding parameters to a command.
/// </summary>
internal static class AddParameterDelegateBuilder<T>
{
    public static Action<DbCommand, T> Build(EntityInfo entity)
    {
        if (entity.Type.ClrType != typeof(T))
            throw new ArgumentException("Entity type mismatch", nameof(entity));

        var command = Expression.Parameter(typeof(DbCommand), "command");
        var value = Expression.Parameter(typeof(T), typeof(T).Name.ToLowerInvariant());

        var block = Expression.Block(AddParameters(command, entity, value));

        return Expression.Lambda<Action<DbCommand, T>>(block, command, value).Compile();
    }

    private static IEnumerable<Expression> AddParameters(ParameterExpression command, IStructureEntity entity, Expression value)
    {
        foreach (var scalar in entity.ScalarProps)
        {
            var parameter = Expression.Variable(typeof(DbParameter), "parameter");
            var parameterName = $"@{scalar.ColumnName.SqlColumnName}";

            var block = new List<Expression>()
            {
                Expression.Assign(
                    parameter,
                    Expression.Call(command, ReflectionItems.CreateParameterMethod)),
                Expression.Assign(
                    Expression.Property(parameter, ReflectionItems.ParameterNameProperty),
                    Expression.Constant(parameterName)),
            };

            var property = Expression.Property(value, scalar.PropertyInfo);
            var assign = Expression.Assign(
                Expression.Property(parameter, ReflectionItems.ParameterValueProperty),
                Expression.Convert(property, typeof(object)));

            var notNull = GetNotNull(property);
            if (notNull == null)
            {
                //No null check, assign the value
                block.Add(assign);
            }
            else
            {
                //Set value only if not null
                block.Add(Expression.IfThen(notNull, assign));
            }

            //Add the parameter to the command
            block.Add(Expression.Call(
                Expression.Property(command, ReflectionItems.CommandParametersProperty),
                ReflectionItems.AddParameterMethod,
                parameter));

            yield return Expression.Block([parameter], block);
        }
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