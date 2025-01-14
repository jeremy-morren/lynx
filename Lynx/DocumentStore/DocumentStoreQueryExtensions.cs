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
}