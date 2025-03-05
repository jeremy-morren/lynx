using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
public static class EntityKeyHelpers
{
    /// <summary>
    /// Gets the value of an entity's primary key.
    /// </summary>
    /// <param name="model">Entity model</param>
    /// <param name="entity">Entity to extract primary key from. Does not have to part of DbContext.</param>
    /// <returns></returns>
    public static object GetEntityKey(this IModel model, object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = entity.GetType();
        var key = (model, entityType);
        var func = Cache.GetOrAdd(key, BuildGetIdValue);
        return func(entity);
    }
    
    private static readonly ConcurrentDictionary<(IModel, Type), Func<object, object>> Cache = new();
    
    private static Func<object, object> BuildGetIdValue((IModel, Type) key) => BuildGetIdValue(key.Item1, key.Item2);
    
    private static Func<object, object> BuildGetIdValue(IModel model, Type type)
    {
        var entity = model.GetEntityType(type);
        var key = entity.GetPrimaryKey();
        
        // Shadow properties are not supported
        if (key.Properties.Any(p => p.IsShadowProperty()))
            throw new NotSupportedException($"Entity {entity} has shadow properties in the key.");

        var param = Expression.Parameter(typeof(object));
        var cast = Expression.Convert(param, entity.ClrType);

        var value = key.Properties.Count switch
        {
            // Single property, return the value directly
            1 => GetPropertyValue(cast, key.Properties[0].PropertyInfo!),
            // Composite key, return a Dictionary<string, object> with the property names and values
            > 1 => CreateDictionary(cast, key.Properties.Select(p => p.PropertyInfo!)),
            // Unknown, throw
            _ => throw new InvalidOperationException()
        };
        //Cast the value to object
        value = Expression.Convert(value, typeof(object));
        return Expression.Lambda<Func<object, object>>(value, param).Compile();
    }

    private static Expression CreateDictionary(Expression source, IEnumerable<PropertyInfo> properties)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

        properties = properties.ToList();

        var dictionary = Expression.Variable(typeof(Dictionary<string, object>));
        var constructor = dictionary.Type.GetConstructor([typeof(int)])!;

        var add = dictionary.Type.GetMethod("Add", flags)!;

        var expressions = new List<Expression>()
        {
            //Optimization: Set capacity to the number of properties
            Expression.Assign(dictionary, Expression.New(constructor, Expression.Constant(properties.Count())))
        };
        expressions.AddRange(
            from p in properties
            let key = Expression.Constant(p.Name)
            let value = GetPropertyValue(source, p)
            select Expression.Call(dictionary, add, key, value));
        
        expressions.Add(dictionary); // Return value
        return Expression.Block([dictionary], expressions);
    }
    
    /// <summary>
    /// Gets property value and casts it to object.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    private static Expression GetPropertyValue(Expression source, PropertyInfo property)
    {
        //Try getting directly from the backing field, if it exists
        var backingField = $"<{property.Name}>k__BackingField";
        var field = property.DeclaringType?.GetField(backingField, BindingFlags.NonPublic | BindingFlags.Instance);
        var member = field != null ? Expression.Field(source, field) : Expression.Property(source, property);
        return Expression.Convert(member, typeof(object));
    }
}