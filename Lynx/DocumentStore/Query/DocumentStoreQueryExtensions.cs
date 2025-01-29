using System.Linq.Expressions;
using System.Reflection;
using Lynx.EfCore;
using Lynx.EfCore.Helpers;
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

        return store.Context.Query<T>()
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

        var query = store.Context.Query<T>().IncludeAllReferenced();
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

        return store.Context.Query<T>();
    }

    private static IQueryable<T> Query<T>(this DbContext context)
        where T : class
    {
        context.Model.GetEntityType(typeof(T)); //Will throw if the entity type is not found

        return context.Set<T>().AsNoTracking();
    }
}