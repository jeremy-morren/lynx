using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

public static class DocumentStoreQueryExtensions
{
    /// <summary>
    /// Queries documents of type <typeparamref name="T"/>. Includes all related entities.
    /// </summary>
    public static IQueryable<T> Query<T>(this IDocumentStore store) where T : class =>
        store.Context.Set<T>().IncludeAll().AsNoTracking();

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id. Includes all related entities.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Task<T?> LoadAsync<T>(this IDocumentStore store, object id, CancellationToken ct) where T : class
    {
        throw new NotImplementedException();
    }
}