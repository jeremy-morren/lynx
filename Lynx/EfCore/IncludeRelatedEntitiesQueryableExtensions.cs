﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
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
        var properties = GetIncludeProperties(query.GetDbContext(), null, typeof(T));
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
        var properties = GetIncludeProperties(query.GetDbContext(), null, typeof(TProperty));

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
        //Get the members of the last include
        var members = query.GetFullIncludeMembers().ToList();

        Debug.Assert(members.Count > 0);

        var properties = GetIncludeProperties(query.GetDbContext(), members[^1], typeof(TProperty));
        var rootPath = string.Join(".", members.Select(m => m.Name));
        return properties.Aggregate<string, IQueryable<TEntity>>(query,
            (current, property) => current.Include($"{rootPath}.{property}"));
    }

    private static readonly ConcurrentDictionary<(IModel, PropertyInfo?, Type), string[]> Cache = new();

    private static string[] GetIncludeProperties(DbContext context, PropertyInfo? parent, Type entityType)
    {
        var key = (context.Model, parent, entityType);

        return Cache.GetOrAdd(key, _ =>
            parent != null
                ? IncludeRelatedEntities.GetIncludeProperties(context.Model, parent, entityType).ToArray()
                : IncludeRelatedEntities.GetIncludeProperties(context.Model, entityType).ToArray());
    }
}