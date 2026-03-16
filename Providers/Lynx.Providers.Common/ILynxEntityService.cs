using System.Data.Common;

namespace Lynx.Providers.Common;

/// <summary>
/// A service for an entity that interacts with a database.
/// </summary>
internal interface ILynxEntityService<in T> where T : class
{
    /// <summary>
    /// Inserts entities into the database.
    /// </summary>
    void Insert(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database.
    /// </summary>
    void Upsert(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database asynchronously.
    /// </summary>
    Task InsertAsync(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database asynchronously.
    /// </summary>
    Task UpsertAsync(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database asynchronously.
    /// </summary>
    Task InsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database asynchronously.
    /// </summary>
    Task UpsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);
}