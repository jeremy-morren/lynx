using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Lynx.EfCore.KeyFilter;

internal static class EntityKeyFilterHelpers
{
    /// <summary>
    /// Filter query by entity key.
    /// </summary>
    public static IQueryable<T> FilterByKey<T>(this IQueryable<T> query, object key) where T : class
    {
        const string methodName = nameof(EntityKeyExpressionBuilders<object, object>.FilterByKey);
        return InvokeGenericMethod<IQueryable<T>>(methodName, typeof(T), key.GetType(), [query, key]);
    }

    /// <summary>
    /// Filter query by entity key.
    /// </summary>
    public static IQueryable<T> FilterByKeys<T>(this IQueryable<T> query, IEnumerable<object> keys) where T : class
    {
        const string methodName = nameof(EntityKeyExpressionBuilders<object, object>.FilterByKeys);

        var list = keys as IReadOnlyCollection<object> ?? keys.ToList();
        if (list.Count == 0)
            return query.Where(_ => false); //Return empty query

        return InvokeGenericMethod<IQueryable<T>>(methodName, typeof(T), GetElementType(list), [query, list]);
    }

    /// <summary>
    /// Gets an expression to filter query by entity key.
    /// </summary>
    public static Expression GetMultipleKeyPredicate<T>(
        List<object> keys,
        ParameterExpression parameter,
        DbContext context) where T : class
    {
        const string methodName = nameof(EntityKeyExpressionBuilders<object, object>.GetMultipleKeyPredicate);
        return InvokeGenericMethod<Expression>(methodName, typeof(T), GetElementType(keys), [keys, parameter, context]);
    }

    #region Reflection

    /// <summary>
    /// Gets value type from a list of items
    /// </summary>
    private static Type GetElementType(IEnumerable<object> items)
    {
        var types = items
            .Select(k => k?.GetType() ?? throw new ArgumentNullException(nameof(items)))
            .Distinct()
            .ToList();
        return types.Count switch
        {
            1 => types[0],
            0 => throw new ArgumentException("Items list must not be empty", nameof(items)),
            _ => throw new ArgumentException("All types must be of the same type", nameof(items))
        };
    }

    private static TReturn InvokeGenericMethod<TReturn>(string methodName, Type entityType, Type keyType, object?[] parameters)
    {
        var key = (methodName, entityType, keyType);
        var func = (Func<object?[], TReturn>)Delegates.GetOrAdd(key,
            _ => BuildInvokeMethod<TReturn>(methodName, entityType, keyType));
        return func(parameters);
    }

    private static readonly ConcurrentDictionary<(string Method, Type EntityType, Type KeyType), object> Delegates = new();

    private static Func<object?[], TReturn> BuildInvokeMethod<TReturn>(string methodName, Type entityType, Type keyType)
    {
        var type = typeof(EntityKeyExpressionBuilders<,>).MakeGenericType(entityType, keyType);
        var method = type.GetMethod(methodName, EntityKeyReflectionHelpers.StaticFlags)
                     ?? throw new InvalidOperationException($"Method {methodName} not found in {type}");

        var param = Expression.Parameter(typeof(object[]));
        var parameters = method.GetParameters()
            .Select(Expression (p) => Expression.Convert(
                Expression.ArrayIndex(param, Expression.Constant(p.Position)),
                p.ParameterType));

        var body = Expression.Call(method, parameters);
        return Expression.Lambda<Func<object?[], TReturn>>(body, param).Compile();
    }

    #endregion
}