using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace Lynx.Provider.Common.Reflection;

internal static class ScalarPropertyHelpers
{
    /// <summary>
    /// Invokes the typed conversion method of a value converter
    /// </summary>
    public static Expression InvokeConverter(ValueConverter converter, Expression expression)
    {
        const string convertToProviderTyped = nameof(ValueConverter<int,int>.ConvertToProviderTyped);

        var convertToPropertyProperty = converter.GetType()
                        .GetProperty(convertToProviderTyped, ReflectionItems.InstanceFlags)
                    ?? throw new InvalidOperationException($"Value converter {converter.GetType()} does not have a typed conversion method");

        var converterValue = Expression.Constant(converter, converter.GetType());
        var convertToProvider = Expression.Property(converterValue, convertToPropertyProperty);

        if (converter.ConvertsNulls)
            // Converter handles nulls, so we can just call the method
            return Expression.Invoke(convertToProvider, expression);

        // Converter does not handle nulls, so we need to check for null

        var isNotNull = GetIfNotNull(expression);
        if (isNotNull == null)
            // Expression is not nullable, no need to check for null
            return Expression.Invoke(convertToProvider, expression);

        // Expression is nullable, so we need to check for null

        if (!expression.Type.IsValueType)
        {
            var invoke = Expression.Invoke(convertToProvider, expression);
            // Reference type, no conversion needed
            return Expression.Condition(isNotNull, invoke, Expression.Constant(null, invoke.Type));
        }

        // Value type, invoke with underlying value and convert back to nullable type
        expression = Expression.Convert(
            Expression.Invoke(convertToProvider, GetNullableValue(expression)),
            expression.Type);

        return Expression.Condition(isNotNull, expression, Expression.Constant(null, expression.Type));
    }

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
    /// Gets the underlying value of a nullable expression
    /// </summary>
    public static Expression GetNullableValue(Expression expression)
    {
        var type = expression.Type;
        if (!type.IsValueType)
            return expression; // Reference type, underlying value is already the value

        Debug.Assert(Nullable.GetUnderlyingType(type) != null, "Value type is not nullable");

        var valueProperty = type.GetProperty(nameof(Nullable<int>.Value), ReflectionItems.InstanceFlags)!;
        return Expression.Property(expression, valueProperty);
    }
}