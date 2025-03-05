using System.Collections.Concurrent;
using System.Linq.Expressions;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Lynx.EfCore;

public static class IncludeRelatedEntitiesQueryableExtensions
{
    /// <summary>
    /// Includes all referenced foreign entities in the query.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IQueryable<T> IncludeAllReferenced<T>(this IQueryable<T> query) where T : class
    {
        var properties = GetIncludeProperties(query.GetDbContext(), typeof(T));
        return properties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
    }

    /// <summary>
    /// Includes all entities referenced by a navigation property in the query.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="selector">Property to include references for</param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IQueryable<TEntity> IncludeAllReferenced<TEntity, TProperty>(this IQueryable<TEntity> query, Expression<Func<TEntity, TProperty?>> selector)
        where TEntity : class
        where TProperty : class
    {
        var properties = GetIncludeProperties(query.GetDbContext(), typeof(TProperty));

        //Get root property path, to prefix all properties
        var rootPath = selector.GetMembers();
        return properties.Aggregate(query, (current, includeProperty) =>
            current.Include($"{rootPath}.{includeProperty}"));
    }

    /// <summary>
    /// Includes all referenced foreign entities in the query.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    public static IQueryable<TEntity> ThenIncludeAllReferenced<TEntity, TProperty>(
        this IIncludableQueryable<TEntity, IEnumerable<TProperty>?> query)
        where TEntity : class
        where TProperty : class
    {
        //TODO: make this work with owned entities

        var properties = GetIncludeProperties(query.GetDbContext(), typeof(TProperty));

        //Get root property path, to prefix all properties
        var rootPath = query.GetFullIncludePath();
        return properties.Aggregate<string, IQueryable<TEntity>>(query,
            (current, property) => current.Include($"{rootPath}.{property}"));
    }

    private static readonly ConcurrentDictionary<(IModel, Type), string[]> Cache = new();

    private static string[] GetIncludeProperties(DbContext context, Type propertyType)
    {
        var key = (context.Model, propertyType);
        return Cache.GetOrAdd(key, _ =>
            IncludeRelatedEntities.GetIncludeProperties(context.Model, propertyType).ToArray());
    }
}