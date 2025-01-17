using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore.OptionalForeign;

public static class OptionalForeignPropertyIncludeExtensions
{
    /// <summary>
    /// Lynx: Includes an optional foreign property (i.e. may exist or not).
    /// </summary>
    public static IQueryable<T> IncludeOptionalForeign<T>(this IQueryable<T> source, string navigationProperty)
        where T : class
    {
        var properties = typeof(T).GetPropertiesMap();

        if (!properties.TryGetValue(navigationProperty, out var property))
            throw new InvalidOperationException($"Property '{navigationProperty}' not found on entity '{typeof(T)}'");

        var method = IncludeOptionalForeignInternalMethod.MakeGenericMethod(typeof(T), property.PropertyType);
        return (IQueryable<T>)method.Invoke(null, [source, navigationProperty])!;
    }

    /// <summary>
    /// Lynx: Includes an optional foreign property (i.e. may exist or not).
    /// </summary>
    public static IQueryable<T> IncludeOptionalForeign<T, TProperty>(this IQueryable<T> source, Expression<Func<T, TProperty?>> navigationPropertyPath)
        where T : class
        where TProperty : class
    {
        var navProperty = OptionalForeignPropertyBuilderExtensions.GetNavigationPropertyName(navigationPropertyPath);

        return IncludeOptionalForeignInternal<T, TProperty>(source, navProperty);
    }

    private static IQueryable<T> IncludeOptionalForeignInternal<T, TProperty>(
        IQueryable<T> source,
        string navigationProperty)
        where T : class
        where TProperty : class
    {
        var context = source.GetDbContext();
        var entity = context.Model.GetEntityType(typeof(T));

        var annotationName = OptionalForeignPropertyBuilderExtensions.GetForeignKeyAnnotation(navigationProperty);
        if (entity.FindAnnotation(annotationName)?.Value is not string foreignKeyPropName)
            throw new InvalidOperationException($"Navigation property '{navigationProperty}' is not an optional foreign property.");

        var foreign = context.Model.GetEntityType(typeof(TProperty));
        var primaryKey = foreign.GetPrimaryKey();
        if (primaryKey.Properties.Count != 1)
            throw new InvalidOperationException("Only simple keys are supported.");

        var primaryKeyProperty = primaryKey.Properties[0];
        var foreignKeyProperty = entity.GetProperty(foreignKeyPropName);

        if (GetPropertyType(foreignKeyProperty) != GetPropertyType(primaryKeyProperty))
            throw new InvalidOperationException("Foreign key and primary key types do not match.");

        var query = LeftJoin<T, TProperty>(
            () => context.Set<TProperty>().AsNoTracking(),
            primaryKeyProperty,
            foreignKeyProperty);

        var copy = CreateCopyExpression<T, TProperty>(entity, navigationProperty);

        return source.SelectMany(query, copy);
    }

    private static Expression<Func<T, IEnumerable<TProperty?>>> LeftJoin<T, TProperty>(
        Expression<Func<IQueryable<TProperty>>> getQuery,
        IProperty primaryKeyProperty,
        IProperty foreignKeyProperty)
        where T : class
        where TProperty : class
    {
        //Result is as follows
        // p => query
        //     .Where(f => EF.Property<object>(p, primaryKeyProperty) == EF.Property<object>(f, foreignKeyProperty))
        //     .DefaultIfEmpty();

        var query = getQuery.Body;

        var principal = Expression.Parameter(typeof(T), "p");
        var foreign = Expression.Parameter(typeof(TProperty), "f");

        //Where filter expression
        var filter = Expression.Lambda<Func<TProperty, bool>>(
            Expression.Equal(
                Expression.Call(
                    EfPropertyMethod.MakeGenericMethod(primaryKeyProperty.ClrType),
                    foreign,
                    Expression.Constant(primaryKeyProperty.Name)),
                Expression.Call(
                    EfPropertyMethod.MakeGenericMethod(foreignKeyProperty.ClrType),
                    principal,
                    Expression.Constant(foreignKeyProperty.Name))
            ),
            foreign);

        //Get the where method
        var whereMethod = QueryableWhereMethods
            .Select(m => m.MakeGenericMethod(typeof(TProperty)))
            .Single(m => m.GetParameters()[1].ParameterType == typeof(Expression<Func<TProperty, bool>>));

        var defaultIfEmpty = DefaultIfEmptyMethod.MakeGenericMethod(typeof(TProperty));
        var body = Expression.Call(
            defaultIfEmpty,
            Expression.Call(
                whereMethod,
                query,
                filter
            ));
        return Expression.Lambda<Func<T, IEnumerable<TProperty?>>>(body, principal);
    }

    /// <summary>
    /// Create a new principal entity with the related entity copied from the source entity.
    /// </summary>
    private static Expression<Func<TPrincipal, TForeign?, TPrincipal>> CreateCopyExpression<TPrincipal, TForeign>(
        IEntityType entity, string relatedProperty)
        where TPrincipal : class
        where TForeign : class
    {
        var constructor = typeof(TPrincipal).GetConstructor([])
            ?? throw new InvalidOperationException($"No parameterless constructor found on entity '{typeof(TPrincipal)}'");

        var properties = typeof(TPrincipal).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        if (properties.All(p => p.Name != relatedProperty))
            throw new InvalidOperationException($"Property '{relatedProperty}' not found on entity '{typeof(TPrincipal)}'");

        var src = Expression.Parameter(typeof(TPrincipal), "src");
        var f = Expression.Parameter(typeof(TForeign), "f");

        var bindings = properties
            .Select(p => p.Name == relatedProperty
                ? Expression.Bind(p, f)
                : Expression.Bind(p, Expression.Property(src, p)))
            .ToList();

        var init = Expression.MemberInit(Expression.New(constructor), bindings);

        return Expression.Lambda<Func<TPrincipal, TForeign?, TPrincipal>>(init, src, f);
    }

    private static Type GetPropertyType(IProperty property) =>
        Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// EF.Property method
    /// </summary>
    private static readonly MethodInfo EfPropertyMethod =
        typeof(EF).GetMethod(nameof(EF.Property), StaticFlags)!;

    private static readonly MethodInfo[] QueryableWhereMethods =
        typeof(Queryable).GetMethods(StaticFlags).Where(m => m.Name == nameof(Queryable.Where)).ToArray();

    private static readonly MethodInfo DefaultIfEmptyMethod =
        typeof(Queryable).GetMethods(StaticFlags)
            .Single(m => m.Name == nameof(Queryable.DefaultIfEmpty) && m.GetParameters().Length == 1);

    private static readonly MethodInfo IncludeOptionalForeignInternalMethod =
        typeof(OptionalForeignPropertyIncludeExtensions).GetMethod(nameof(IncludeOptionalForeignInternal), StaticFlags)!;


    private static Dictionary<string, PropertyInfo> GetPropertiesMap(this Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        return PropertyCache.GetOrAdd(type, t => t.GetProperties(flags).ToDictionary(p => p.Name));
    }

    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new();
}