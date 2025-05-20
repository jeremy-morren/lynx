using System.Diagnostics;
using System.Linq.Expressions;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace Lynx.Providers.Common.Reflection;

internal static class ExpressionHelpers
{
    /// <summary>
    /// Returns true if the given expression is nullable
    /// </summary>
    public static bool IsNullable(Expression expression)
    {
        if (expression.Type.IsValueType)
            return Nullable.GetUnderlyingType(expression.Type) != null;

        return true;
    }

    /// <summary>
    /// Returns an expression that checks if the given expression is not null, or null if the expression is not nullable.
    /// </summary>
    public static Expression? GetIfNotNull(Expression expression)
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

    /// <summary>
    /// Gets the underlying value of a nullable expression, if the expression is nullable.
    /// </summary>
    public static Expression GetNullableValue(Expression expression)
    {
        var type = expression.Type;
        if (!type.IsValueType)
            return expression; // Reference type, underlying value is already the value

        if (Nullable.GetUnderlyingType(type) == null)
            return expression; // Value type is not nullable

        var valueProperty = type.GetProperty(nameof(Nullable<int>.Value), ReflectionItems.InstanceFlags)!;
        return Expression.Property(expression, valueProperty);
    }

    /// <summary>
    /// Gets the type of the first parameter of a Func{,} expression
    /// </summary>
    public static Type GetFunctionParameter(Expression expression)
    {
        Debug.Assert(expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Func<,>),
            "Input is not a Func<,>");

        return expression.Type.GetGenericArguments()[0];
    }

    /// <summary>
    /// Wraps an action in a TryFinally block with dispose logic.
    /// </summary>
    public static Expression UsingExpression(
        ParameterExpression value,
        Expression @new,
        Func<Expression> action)
    {
        var assign = Expression.Assign(value, @new);
        var nullCheck = GetIfNotNull(value);
        Expression dispose = Expression.Call(value, ReflectionItems.DisposeMethod);
        if (nullCheck != null)
            dispose = Expression.IfThen(nullCheck, dispose);
        return Expression.Block(
            [value],
            assign,
            Expression.TryFinally(action(), dispose));
    }

    /// <summary>
    /// Creates a for loop that iterates over a collection of type IReadOnlyList{T}
    /// </summary>
    public static Expression ForLoop(Expression list, ParameterExpression item, Expression body)
    {
        var @interface = IsIReadOnlyList(list.Type)
            ? list.Type
            : list.Type.GetInterfaces().SingleOrDefault(IsIReadOnlyList);
        Debug.Assert(@interface != null, "Collection must implement IReadOnlyList<T>");

        var readOnlyListType = typeof(IReadOnlyList<>).MakeGenericType(@interface.GetGenericArguments());
        var readOnlyCollectionType = typeof(IReadOnlyCollection<>).MakeGenericType(@interface.GetGenericArguments());

        var getItem = readOnlyListType.GetMethod("get_Item", [typeof(int)])!;
        Debug.Assert(getItem != null, "getItem != null");

        var readOnlyList = Expression.Parameter(readOnlyListType, "list");
        var count = Expression.Variable(typeof(int), "count");
        var i = Expression.Variable(typeof(int), "i");

        var label = Expression.Label();

        return Expression.Block(
            [i, count, readOnlyList, item],
            Expression.Assign(i, Expression.Constant(0)),
            Expression.Assign(readOnlyList, Expression.Convert(list, readOnlyListType)),
            Expression.Assign(count,
                Expression.Property(
                    Expression.Convert(list, readOnlyCollectionType),
                    "Count")),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(i, count),
                    Expression.Block(
                        Expression.Assign(item,
                            Expression.Call(readOnlyList, getItem, i)),
                        body,
                        Expression.PreIncrementAssign(i)),
                    Expression.Break(label)
                ),
                label)
        );

        static bool IsIReadOnlyList(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
    }
}