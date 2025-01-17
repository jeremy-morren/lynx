using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Query;

public static class DocumentStoreLoadExtensions
{
    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id.
    /// Includes all related entities.
    /// Includes entities marked as deleted.
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

        return store.FilterByKey<T>(id, true).SingleOrDefault();
    }

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id.
    /// Includes all related entities.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id"></param>
    /// <param name="includeDeleted">Whether the entity should be returned even if it is deleted</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static T? Load<T>(this IDocumentStore store, object id, bool includeDeleted) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        return store.FilterByKey<T>(id, includeDeleted).SingleOrDefault();
    }

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id.
    /// Includes all related entities.
    /// Includes entities marked as deleted.
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

        return store.FilterByKey<T>(id, true).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id. Includes all related entities.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id"></param>
    /// <param name="includeDeleted">Whether the entity should be returned even if it is deleted</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Task<T?> LoadAsync<T>(this IDocumentStore store, object id, bool includeDeleted, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        return store.FilterByKey<T>(id, includeDeleted).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Filters a query by the key of the entity. Includes related entities and entities marked as deleted.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id">Id value</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IQueryable<T> FilterByKey<T>(this IDocumentStore store, object id) where T : class
    {
        return store.FilterByKey<T>(id, true);
    }

    /// <summary>
    /// Filters a query by the key of the entity. Includes related entities
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id">Id value</param>
    /// <param name="includeDeleted">Whether the entity should be included even if it is deleted</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IQueryable<T> FilterByKey<T>(this IDocumentStore store, object id, bool includeDeleted) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        var query = store.Query<T>(includeDeleted);

        return query.FilterByKey(store.Context, id);
    }
}