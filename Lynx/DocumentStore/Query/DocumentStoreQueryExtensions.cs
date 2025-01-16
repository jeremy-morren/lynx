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
    /// Includes deleted entities.
    /// Does not include related entities.
    /// </summary>
    public static IQueryable<T> QueryRaw<T>(this IDocumentStore store) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);

        return store.Context.CreateLynxQueryable<T>();
    }

    /// <summary>
    /// Loads a document of type <typeparamref name="T"/> by its id. Includes all related entities.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static T? Get<T>(this IDocumentStore store, object id) where T : class
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(id);

        return store.FilterByKey<T>(id).SingleOrDefault();
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

        return query.Where(x => EF.Property<bool>(x, property.Name) == false);
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

        return store.QueryRaw<T>().IncludeAllReferenced().FilterByKey(store.Context, id);
    }
}