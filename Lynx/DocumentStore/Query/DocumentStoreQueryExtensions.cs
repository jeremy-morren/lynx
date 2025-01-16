using System.Linq.Expressions;
using System.Reflection;
using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Query;

public static class DocumentStoreQueryExtensions
{
    /// <summary>
    /// Queries documents of type <typeparamref name="T"/>.
    /// Excludes deleted entities.
    /// Includes all related entities.
    /// </summary>
    public static IQueryable<T> Query<T>(this IDocumentStore store) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);

        return store.Context.CreateLynxQueryable<T>()
            .IncludeAllReferenced()
            .ExcludeDeleted();
    }

    /// <summary>
    /// Queries documents of type <typeparamref name="T"/>.
    /// Includes all related entities.
    /// </summary>
    public static IQueryable<T> Query<T>(this IDocumentStore store, bool includeDeleted) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);

        var query = store.Context.CreateLynxQueryable<T>().IncludeAllReferenced();
        return includeDeleted ? query : query.ExcludeDeleted();
    }

    /// <summary>
    /// Queries documents of type <typeparamref name="T"/>.
    /// Includes deleted entities.
    /// Does not include related entities.
    /// </summary>
    public static IQueryable<T> QueryRaw<T>(this IDocumentStore store) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);

        return store.Context.CreateLynxQueryable<T>();
    }

    /// <summary>
    /// Excludes deleted entities from the query.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IQueryable<T> ExcludeDeleted<T>(this IQueryable<T> query) where T : class
    {
        var context = query.GetDbContext();
        var entityType = context.Model.GetEntityType(typeof(T));

        var property = entityType.FindProperty("Deleted") ?? entityType.FindProperty("IsDeleted");
        if (property?.ClrType != typeof(bool) && property?.ClrType != typeof(bool?))
            return query; //No deleted property found, ignore

        //For readability of query debug view, write lambda to use a constant value for name (instead of a captured variable)
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.Equal(
            Expression.Call(
                BoolPropertyMethod.MakeGenericMethod(property.ClrType),
                param,
                Expression.Constant(property.Name)),
            Expression.Constant(false)
        );
        var lambda = Expression.Lambda<Func<T, bool>>(body, param);

        return query.Where(lambda);
    }

    private static readonly MethodInfo BoolPropertyMethod = typeof(EF)
        .GetMethod(nameof(EF.Property), BindingFlags.Public | BindingFlags.Static)!;
}