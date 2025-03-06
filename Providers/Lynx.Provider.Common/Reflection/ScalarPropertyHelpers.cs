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

        var ifNotNull = GetIfNotNull(expression);
        if (ifNotNull == null)
            // Expression is not nullable, no need to check for null
            return Expression.Invoke(convertToProvider, expression);

        // Expression is nullable
        // We need to check for null before calling the converter
        // The resulting expression must be a nullable type

        if (!expression.Type.IsValueType)
        {
            // Input is reference type, no need to get underlying value
            var invoke = Expression.Invoke(convertToProvider, expression);
            if (!invoke.Type.IsValueType || Nullable.GetUnderlyingType(invoke.Type) != null)
            {
                // Output is reference type or Nullable<>, no conversion needed
                return Expression.Condition(ifNotNull, invoke, Expression.Constant(null, invoke.Type));
            }
            // Output is non-null value type, convert to Nullable<>
            var nullable = typeof(Nullable<>).MakeGenericType(invoke.Type);
            return Expression.Condition(ifNotNull, 
                Expression.Convert(invoke, nullable), 
                Expression.Constant(null, nullable));
        }
        else
        {
            // Input is Nullable<>
            Debug.Assert(Nullable.GetUnderlyingType(expression.Type) != null);
            
            var invoke = Expression.Invoke(convertToProvider, GetNullableValue(expression));

            if (!invoke.Type.IsValueType || Nullable.GetUnderlyingType(invoke.Type) != null)
            {
                //Output is reference type or Nullable<>, no conversion needed
                return Expression.Condition(ifNotNull, invoke, Expression.Constant(null, invoke.Type));
            }
            
            //Output is non-null value type, convert to Nullable<>
            
            var nullable = typeof(Nullable<>).MakeGenericType(invoke.Type);
            return Expression.Condition(
                ifNotNull, 
                Expression.Convert(invoke, nullable), 
                Expression.Constant(null, nullable));
        }
        
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
    private static Expression GetNullableValue(Expression expression)
    {
        var type = expression.Type;
        if (!type.IsValueType)
            return expression; // Reference type, underlying value is already the value

        Debug.Assert(Nullable.GetUnderlyingType(type) != null, "Value type is not nullable");

        var valueProperty = type.GetProperty(nameof(Nullable<int>.Value), ReflectionItems.InstanceFlags)!;
        return Expression.Property(expression, valueProperty);
    }
}