using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

internal static class EntityKeyFilterHelpers
{
    /// <summary>
    /// Filter query by entity key.
    /// </summary>
    public static IQueryable<T> FilterByKey<T>(this IQueryable<T> query, DbContext context, object key) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);

        var properties = GetKeyNames(typeof(T), context);

        if (properties.Count == 1)
        {
            //Single key
            return query.Where(x => EF.Property<object>(x, properties[0]) == key);
        }

        switch (properties.Count)
        {
            case 1:
                //Single key
                return query.Where(x => EF.Property<object>(x, properties[0]) == key);

            case > 1:
                //Composite key
                foreach (var (property, getValue) in GetKeyValues(context.Model, typeof(T), key.GetType(), properties))
                {
                    var value = getValue(key);
                    query = query.Where(x => EF.Property<object>(x, property) == value);
                }
                return query;
            default:
                throw new InvalidOperationException($"Entity {typeof(T)} key has no properties");
        }
    }

    private static List<string> GetKeyNames(Type type, DbContext context)
    {
        var entity = context.Model.GetEntityType(type);
        var key = entity.GetPrimaryKey();

        return key.Properties.Select(p => p.Name).ToList();
    }

    //Composite keys: Build list of property names and value getters for each property

    /// <summary>
    /// Map of entity and key types to list of property names and value getters for each property.
    /// </summary>
    private static readonly ConcurrentDictionary<(IModel, Type EntityType, Type KeyType), List<(string Property, Func<object, object>)>> GetKeyValuesCache = new ();

    private static List<(string Property, Func<object, object>)> GetKeyValues(IModel model, Type entityType, Type keyType, List<string> properties)
    {
        if (keyType.IsPrimitive)
            throw new InvalidOperationException($"Entity {entityType} has a composite key. Provided key: {keyType}");

        var key = (model, entityType, keyType);
        return GetKeyValuesCache.GetOrAdd(key, _ => BuildGetKeyValues(keyType, properties).ToList());
    }

    private static IEnumerable<(string Property, Func<object, object>)> BuildGetKeyValues(Type keyType, List<string> properties)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var props = keyType.GetProperties(flags).ToDictionary(p => p.Name);
        if (props.Count != properties.Count)
            throw new InvalidOperationException($"Composite key properties count mismatch. Expected: {properties.Count}, Actual: {props.Count}");

        foreach (var p in properties)
        {
            if (!props.TryGetValue(p, out var propInfo))
                throw new InvalidOperationException($"Composite key property '{p}' not found on composite id '{keyType}'");

            var param = Expression.Parameter(typeof(object), "key");

            var func = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(param, keyType), propInfo),
                        typeof(object)),
                    param)
                .Compile();
            yield return (p, func);
        }
    }
}