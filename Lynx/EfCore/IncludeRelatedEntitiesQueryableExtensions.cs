using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
    public static IQueryable<T> IncludeAll<T>(this IQueryable<T> query) where T : class
    {
        var context = query.GetDbContext(nameof(IncludeAll));
        var key = (context.Model, typeof(T));
        var properties = Cache.GetOrAdd(key, GetIncludeProperties);
        return properties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
    }

    private static readonly ConcurrentDictionary<(IModel, Type), string[]> Cache = new();

    private static string[] GetIncludeProperties((IModel, Type) key) => 
        IncludeRelatedEntities.GetIncludeProperties(key.Item1, key.Item2).ToArray();
}