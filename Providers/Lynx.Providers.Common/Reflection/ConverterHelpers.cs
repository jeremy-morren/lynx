using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable ConvertIfStatementToReturnStatement

namespace Lynx.Providers.Common.Reflection;

internal static class ConverterHelpers
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

        var ifNotNull = ExpressionHelpers.GetIfNotNull(expression);
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

            // NB: Converter does not handle nulls, but may still accept nullable input

            var value = ExpressionHelpers.GetNullableValue(expression);
            var functionParameter = ExpressionHelpers.GetFunctionParameter(convertToProvider);
            if (Nullable.GetUnderlyingType(functionParameter) != null)
            {
                // Converter accepts nullable input, need to convert input back to nullable
                Debug.Assert(expression.Type == functionParameter, "Converter input type mismatch");
                value = Expression.Convert(value, functionParameter);
            }

            var invoke = Expression.Invoke(convertToProvider, value);

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
}