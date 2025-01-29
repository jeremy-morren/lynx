using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.DocumentStore.Query;

public static class QueryableSoftDeleteExtensions
{
    /// <summary>
    /// Excludes deleted entities from the query.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IQueryable<T> ExcludeDeleted<T>(this IQueryable<T> query) where T : class
    {
        var model = query.GetDbContext().Model;
        if (GetExcludeDeleted<T>(model) is { } excludeDeleted)
            return query.Where(excludeDeleted);
        return query;
    }

    private static Expression<Func<T, bool>>? GetExcludeDeleted<T>(IModel model) where T : class
    {
        var key = (model, typeof(T));
        var lambda = DeletedLambdaCache.GetOrAdd(key, _ => BuildExcludeDeleted<T>(model));
        return (Expression<Func<T, bool>>?)lambda;
    }

    private static Expression<Func<T, bool>>? BuildExcludeDeleted<T>(IModel model) where T : class
    {
        var entityType = model.GetEntityType(typeof(T));

        var property = entityType.FindProperty("Deleted") ?? entityType.FindProperty("IsDeleted");
        if (property?.ClrType != typeof(bool) && property?.ClrType != typeof(bool?))
            return null; //No deleted property found, ignore

        var param = Expression.Parameter(typeof(T), typeof(T).Name.ToLowerInvariant());

        var body = property.PropertyInfo != null
            //Generate x => x.Deleted == false
            ? Expression.Equal(
                Expression.Property(param, property.PropertyInfo),
                Expression.Constant(false)
            )
            //Deleted property is a shadow property
            : Expression.Equal(
                Expression.Call(
                    BoolPropertyMethod.MakeGenericMethod(property.ClrType),
                    param,
                    Expression.Constant(property.Name)),
                Expression.Constant(false)
            );

        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    /// <summary>
    /// Map of cached lambda expressions to exclude deleted entities, or null if no deleted property is found.
    /// </summary>
    private static readonly ConcurrentDictionary<(IModel, Type), object?> DeletedLambdaCache = new();

    private static readonly MethodInfo BoolPropertyMethod = typeof(EF)
        .GetMethod(nameof(EF.Property), BindingFlags.Public | BindingFlags.Static)!;
}