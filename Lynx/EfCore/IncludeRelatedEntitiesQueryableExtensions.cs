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
        var context = query.GetDbContext();
        var key = (context.Model, typeof(T));
        var properties = Cache.GetOrAdd(key, GetIncludeProperties);
        return properties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
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

        var context = query.GetDbContext();

        var key = (context.Model, typeof(TProperty));
        var properties = Cache.GetOrAdd(key, GetIncludeProperties);

        //Get root property path
        var rootPath = query.GetFullIncludePath();
        return properties.Aggregate<string, IQueryable<TEntity>>(query,
            (current, property) => current.Include($"{rootPath}.{property}"));
    }

    private static readonly ConcurrentDictionary<(IModel, Type), string[]> Cache = new();

    private static string[] GetIncludeProperties((IModel, Type) key) => 
        IncludeRelatedEntities.GetIncludeProperties(key.Item1, key.Item2).ToArray();
}