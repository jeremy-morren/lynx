using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

public static class DocumentStoreQueryExtensions
{
    /// <summary>
    /// Queries documents of type <typeparamref name="T"/>. Includes all related entities.
    /// </summary>
    public static IQueryable<T> Query<T>(this IDocumentStore store) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);

        return store.Context.Set<T>().IncludeAll().AsNoTracking();
    }

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id. Includes all related entities.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static T? Load<T>(this IDocumentStore store, object id) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        return store.FilterByKey<T>(id).SingleOrDefault();
    }

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id. Includes all related entities.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Task<T?> LoadAsync<T>(this IDocumentStore store, object id, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        return store.FilterByKey<T>(id).SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<T> FilterByKey<T>(this IDocumentStore store, object id) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        return store.Query<T>().FilterByKey(store.Context, id);
    }
}