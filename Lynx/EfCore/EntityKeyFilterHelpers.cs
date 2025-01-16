using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

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
        switch (properties.Length)
        {
            case 1:
                //Single key
                return query.Where(x => EF.Property<object>(x, properties[0]) == key);

            case > 1:
                //Composite key
                foreach (var (property, getValue) in GetKeyValues(typeof(T), key.GetType(), properties))
                {
                    var value = getValue(key);
                    query = query.Where(x => EF.Property<object>(x, property) == value);
                }

                return query;
            default:
                throw new InvalidOperationException($"Entity {typeof(T)} key has no properties");
        }
    }

    private static string[] GetKeyNames(Type type, DbContext context)
    {
        var entity = context.Model.GetEntityType(type);
        var key = entity.GetPrimaryKey();

        return key.Properties.Select(p => p.Name).ToArray();
    }

    //Composite keys: Build list of property names and value getters for each property

    /// <summary>
    /// Map of entity and key types to list of property names and value getters for each property.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type EntityType, Type KeyType), List<(string Property, Func<object, object>)>> GetKeyValuesCache = new ();

    private static List<(string Property, Func<object, object>)> GetKeyValues(Type entityType, Type keyType, string[] properties)
    {
        var key = (entityType, keyType);
        return GetKeyValuesCache.GetOrAdd(key, _ => BuildGetKeyValues(keyType, properties));
    }

    private static List<(string Property, Func<object, object>)> BuildGetKeyValues(Type keyType, string[] properties)
    {
        return properties
            .Select(property => (property, BuildGetValueFunc(property, keyType)))
            .ToList();
    }

    private static Func<object, object> BuildGetValueFunc(string property, Type keyType)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var propInfo = keyType.GetProperty(property, flags)
                       ?? throw new InvalidOperationException($"Composite key property '{property}' not found on composite id '{keyType}'");


        var param = Expression.Parameter(typeof(object), "key");

        return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(param, keyType), propInfo),
                    typeof(object)),
                param)
            .Compile();
    }
}