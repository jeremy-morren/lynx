using System.Data.Common;

namespace Lynx.Providers.Common;

/// <summary>
/// A service for an entity that interacts with a database.
/// </summary>
internal interface ILynxDatabaseService<in T> where T : class
{
    /// <summary>
    /// Inserts entities into the database.
    /// </summary>
    public void Insert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database.
    /// </summary>
    public void Upsert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts entities into the database.
    /// </summary>
    public void BulkInsert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts entities into the database.
    /// </summary>
    public void BulkUpsert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database asynchronously.
    /// </summary>
    public Task InsertAsync(
        IEnumerable<T> entities,DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database asynchronously.
    /// </summary>
    public Task UpsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database in bulk.
    /// </summary>
    public Task BulkInsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates entities into the database in bulk.
    /// </summary>
    public Task BulkUpsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);
}