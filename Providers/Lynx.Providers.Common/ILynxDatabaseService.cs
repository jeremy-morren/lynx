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
    void Insert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database.
    /// </summary>
    void Upsert(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database asynchronously.
    /// </summary>
    Task InsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database asynchronously.
    /// </summary>
    Task UpsertAsync(
        IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default);

}

/// <summary>
/// A service for an entity that interacts with a database in bulk.
/// </summary>
internal interface ILynxDatabaseServiceBulk<in T> : ILynxDatabaseService<T> where T : class
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
}