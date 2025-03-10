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
    /// Returns an expression that checks if the given expression is not null null, or null if the expression is not nullable.
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
}