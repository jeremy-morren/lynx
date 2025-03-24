using System.Data.Common;

namespace Lynx.Providers.Common;

/// <summary>
/// A service for an entity that interacts with a database in bulk.
/// </summary>
internal interface ILynxEntityServiceBulk<in T> : ILynxEntityService<T> where T : class
{
    /// <summary>
    /// Bulk inserts entities into the database.
    /// </summary>
    void BulkInsert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts entities into the database.
    /// </summary>
    void BulkUpsert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts entities into the database.
    /// </summary>
    Task BulkInsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts entities into the database.
    /// </summary>
    Task BulkUpsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts entities into the database.
    /// </summary>
    Task BulkInsertAsync(
        IAsyncEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts entities into the database.
    /// </summary>
    Task BulkUpsertAsync(
        IAsyncEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);
}