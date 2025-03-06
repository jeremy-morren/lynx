using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Reflection;

namespace Lynx.Provider.Sqlite;

internal class SqliteDbJsonMapper : IDbJsonMapper
{
    // Microsoft.Data.Sqlite has no built in support for JSON, so we have to serialize it ourselves


    public static Expression SetupJsonParameter(Expression command)
    {
        // Set parameter type to string
        return Expression.Assign(
            Expression.Property(command, ReflectionItems.DbParameterDbTypeProperty),
            Expression.Constant(DbType.String, typeof(DbType)));
    }

    public static Expression CreateJsonValue(Expression value)
    {
        var options = Expression.Property(null, DefaultJsonSerializerOptions);
        var serializeMethod = SerializeMethod.MakeGenericMethod(value.Type); // JsonSerializer.Serialize<T>(T, JsonSerializerOptions)

        var serialize = Expression.Call(null, serializeMethod, value, options);
        // Check value for null
        var ifNotNull = ScalarPropertyHelpers.GetIfNotNull(value);
        if (ifNotNull == null)
            return serialize; // Value is not nullable, no need to check for null

        // Value is nullable, check for null
        return Expression.Condition(ifNotNull, serialize, Expression.Constant(null, serialize.Type));
    }

    /// <summary>
    /// <see cref="JsonSerializerOptions.Default"/>
    /// </summary>
    private static readonly PropertyInfo DefaultJsonSerializerOptions =
        typeof(JsonSerializerOptions).GetProperty(nameof(JsonSerializerOptions.Default), ReflectionItems.StaticFlags)!;

    /// <summary>
    /// <see cref="JsonSerializer.Serialize{T}(T,JsonSerializerOptions)"/>
    /// </summary>
    private static readonly MethodInfo SerializeMethod =
        typeof(JsonSerializer).GetMethods(ReflectionItems.StaticFlags)
            .Single(m =>
                m is
                {
                    Name: nameof(JsonSerializer.Serialize),
                    IsGenericMethodDefinition: true
                }
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType.IsGenericParameter
                && m.GetParameters()[1].ParameterType == typeof(JsonSerializerOptions));
}