using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

namespace Lynx.EfCore.KeyFilter;

/// <summary>
/// A collection of expression builders for filtering entities by key.
/// </summary>
internal static class EntityKeyExpressionBuilders<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Filter query by entity key.
    /// </summary>
    public static IQueryable<TEntity> FilterByKey(IQueryable<TEntity> query, object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key is not TKey typedKey)
            throw new ArgumentException($"Key must be of type {typeof(TKey)}", nameof(key));
        var model = query.GetDbContext().Model;
        var properties = GetKeyProperties(model);

        var predicate = properties.Count switch
        {
            1 => GetScalarKeyPredicate(typedKey, model),
            > 1 => GetCompositeKeyPredicate(typedKey, model),
            _ => throw new InvalidOperationException($"Entity {typeof(TEntity)} key has no properties")
        };
        query = query.Where(predicate);
        return query;
    }

    /// <summary>
    /// Filter query by entity key.
    /// </summary>
    public static IQueryable<TEntity> FilterByKeys(IQueryable<TEntity> query, IEnumerable<object> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var list = keys as IReadOnlyCollection<TKey> ?? keys.Cast<TKey>().ToList();
        if (list.Count == 0)
            return query.Where(_ => false); //Return empty query

        var model = query.GetDbContext().Model;
        var properties = GetKeyProperties(model);

        var predicate = properties.Count switch
        {
            1 => GetMultipleScalarKeyPredicate(list, model),
            > 1 => GetMultipleCompositeKeyPredicate(list, model),
            _ => throw new InvalidOperationException($"Entity {typeof(TKey)} key has no properties")
        };
        query = query.Where(predicate);
        return query;
    }

    /// <summary>
    /// Gets an expression to filter query by entity key.
    /// </summary>
    public static Expression GetMultipleKeyPredicate(IEnumerable<object> keys, ParameterExpression parameter, DbContext context)
    {
        if (parameter.Type != typeof(TEntity))
            throw new ArgumentException("Parameter type must match entity type", nameof(parameter));

        ArgumentNullException.ThrowIfNull(keys);

        var list = keys as IReadOnlyCollection<TKey> ?? keys.Cast<TKey>().ToList();
        if (list.Count == 0)
            throw new ArgumentOutOfRangeException(nameof(keys));

        var model = context.Model;
        var properties = GetKeyProperties(model);

        return properties.Count switch
        {
            1 => GetMultipleScalarKeyPredicate(list, parameter, model),
            > 1 => GetMultipleCompositeKeyPredicate(list, parameter, model),
            _ => throw new InvalidOperationException($"Entity {typeof(TEntity)} key has no properties")
        };
    }

    /// <summary>
    /// Gets key property names for an entity type.
    /// </summary>
    private static IReadOnlyList<IProperty> GetKeyProperties(IModel model)
    {
        var entity = model.GetEntityType(typeof(TEntity));
        return entity.GetPrimaryKey().Properties;
    }

    #region Scalar Keys

    private static Expression<Func<TEntity, bool>> GetScalarKeyPredicate(TKey key, IModel model)
    {
        ArgumentNullException.ThrowIfNull(key);

        var property = GetKeyProperties(model).Single();
        var keyExpression = ExpressionCaptureValue.CaptureValue(key);

        var param = Expression.Parameter(typeof(TEntity), "x");
        var call = Expression.Call(EfPropertyTKeyMethod, param, Expression.Constant(property.Name));
        var equal = Expression.Equal(call, keyExpression);
        return Expression.Lambda<Func<TEntity, bool>>(equal, param);
    }

    private static Expression<Func<TEntity, bool>> GetMultipleScalarKeyPredicate(IEnumerable<TKey> keys, IModel model)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var contains = GetMultipleScalarKeyPredicate(keys, parameter, model);
        return Expression.Lambda<Func<TEntity, bool>>(contains, parameter);
    }

    private static MethodCallExpression GetMultipleScalarKeyPredicate(
        IEnumerable<TKey> keys, ParameterExpression parameter, IModel model)
    {
        var property = GetKeyProperties(model).Single();

        //Use a keys.Contains(x.Property) expression

        var keysExpression = ExpressionCaptureValue.CaptureValue(keys);
        var propertyExpression = Expression.Call(EfPropertyTKeyMethod, parameter, Expression.Constant(property.Name));
        return Expression.Call(EnumerableContainsTKeyMethod, keysExpression, propertyExpression);
    }

    #endregion

    #region Composite Keys

    //Composite keys: Build list of property names and value getters for each property

    private static Expression<Func<TEntity, bool>> GetCompositeKeyPredicate(TKey key, IModel model)
    {
        ArgumentNullException.ThrowIfNull(key);

        var param = Expression.Parameter(typeof(TEntity), "x");
        var body = BuildMatchCompositeKeyExpression(param, key, model);
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    private static Expression<Func<TEntity, bool>> GetMultipleCompositeKeyPredicate(IReadOnlyCollection<TKey> keys, IModel model)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var predicate = GetMultipleCompositeKeyPredicate(keys, parameter, model);
        return Expression.Lambda<Func<TEntity, bool>>(predicate, parameter);
    }

    private static Expression GetMultipleCompositeKeyPredicate(
        IReadOnlyCollection<TKey> keys, ParameterExpression parameter, IModel model)
    {
        var expressions = keys
            .Select(key => BuildMatchCompositeKeyExpression(parameter, key, model))
            .ToList();

        //You can't do an IN statement on multiple columns, so we expand the keys into OR statements
        return expressions.Aggregate(Expression.OrElse);
    }

    private static Expression BuildMatchCompositeKeyExpression(ParameterExpression parameter, TKey key, IModel model)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (parameter.Type != typeof(TEntity))
            throw new ArgumentException("Parameter type must match entity type", nameof(parameter));

        var properties = GetCompositeKeyProperties(model);

        //Expression: x.P1 == key.P1 && x.P2 == key.P2 && ...

        var expressions = new List<Expression>(properties.Count);

        foreach (var (property, efPropertyMethod, getValue) in properties)
        {
            var name = Expression.Constant(property.Name);
            var value = getValue(key);
            var call = Expression.Call(efPropertyMethod, parameter, name);
            var equal = Expression.Equal(call, value);
            expressions.Add(equal);
        }

        return expressions.Aggregate(Expression.AndAlso);
    }

    private class CompositeKeyProperty
    {
        public CompositeKeyProperty(IProperty property, Func<TKey, Expression> getCapturedValue)
        {
            Property = property;
            GetCapturedValue = getCapturedValue;
            EfPropertyMethod = EntityKeyReflectionHelpers.EfPropertyMethod.MakeGenericMethod(property.ClrType);
        }

        public IProperty Property { get; }
        public MethodInfo EfPropertyMethod { get; }
        public Func<TKey, Expression> GetCapturedValue { get; }

        public void Deconstruct(
            out IProperty property,
            out MethodInfo efPropertyMethod,
            out Func<TKey, Expression> getCapturedValue)
        {
            property = Property;
            efPropertyMethod = EfPropertyMethod;
            getCapturedValue = GetCapturedValue;
        }
    }

    /// <summary>
    /// Returns delegates to get captured key values from a composite key.
    /// </summary>
    private static List<CompositeKeyProperty> GetCompositeKeyProperties(IModel model)
    {
        if (typeof(TKey).IsPrimitive || typeof(TKey) == typeof(string))
            throw new InvalidOperationException($"Entity {typeof(TEntity)} has a composite key. Provided key: {typeof(TKey)}");

        return GetKeyValuesCache.GetOrAdd(model, BuildCompositeKeyProperties);
    }

    /// <summary>
    /// Map of entity and key types to list of property names and value getters for each property.
    /// </summary>
    private static readonly ConcurrentDictionary<IModel, List<CompositeKeyProperty>> GetKeyValuesCache = new ();

    /// <summary>
    /// Builds delegates to get captured key values from a composite key.
    /// </summary>
    private static List<CompositeKeyProperty> BuildCompositeKeyProperties(IModel model)
    {
        var properties = GetKeyProperties(model);

        var param = Expression.Parameter(typeof(TKey), "key");

        var result = new List<CompositeKeyProperty>();

        if (typeof(TKey).IsAssignableTo(EntityKeyReflectionHelpers.DictionaryType))
        {
            // TKey is a dictionary
            var getItem = EntityKeyReflectionHelpers.GetGetItemMethod(typeof(TKey));

            foreach (var p in properties)
            {
                var capture = EntityKeyReflectionHelpers.GetCaptureValueMethod(p.ClrType);
                var name = Expression.Constant(p.Name);
                Expression body = Expression.Convert(Expression.Call(param, getItem, name), p.ClrType);
                body = Expression.Call(capture, body);
                var func = Expression.Lambda<Func<TKey, Expression>>(body, param).Compile();
                result.Add(new(p, func));
            }
        }
        else
        {
            //TKey is a composite key object (possibly anonymous type)

            var props = typeof(TKey)
                .GetProperties(InstanceFlags)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            if (props.Count != properties.Count)
                throw new InvalidOperationException($"Composite key properties count mismatch. Expected: {properties.Count}, Actual: {props.Count}");

            foreach (var p in properties)
            {
                if (!props.TryGetValue(p.Name, out var propInfo))
                    throw new InvalidOperationException($"Composite key property '{p.Name}' not found on composite id '{typeof(TKey)}'");

                if (propInfo.PropertyType != p.ClrType)
                    throw new InvalidOperationException($"Composite key property '{p.Name}' type mismatch. Expected: {p.ClrType}, Actual: {propInfo.PropertyType}");

                var captureValue = EntityKeyReflectionHelpers.GetCaptureValueMethod(propInfo.PropertyType);

                //Invoke capture value on the property
                var body = Expression.Call(captureValue, Expression.Property(param, propInfo));
                var func = Expression.Lambda<Func<TKey, Expression>>(body, param).Compile();
                result.Add(new(p, func));
            }
        }
        return result;
    }

    #endregion

    #region Reflection

    private const BindingFlags InstanceFlags = EntityKeyReflectionHelpers.InstanceFlags;

    /// <summary>
    /// EF.Property{TKey} method.
    /// </summary>
    private static readonly MethodInfo EfPropertyTKeyMethod =
        EntityKeyReflectionHelpers.EfPropertyMethod.MakeGenericMethod(typeof(TKey));

    /// <summary>
    /// Enumerable.Contains{TKey} method.
    /// </summary>
    private static readonly MethodInfo EnumerableContainsTKeyMethod =
        EntityKeyReflectionHelpers.EnumerableContainsMethod.MakeGenericMethod(typeof(TKey));

    #endregion

}