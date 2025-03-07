using System.Data.Common;

namespace Lynx.Provider.Common;

/// <summary>
/// A service for an entity that interacts with a database.
/// </summary>
internal interface ILynxDatabaseService<in T> where T : class
{
    /// <summary>
    /// Inserts entities into the database.
    /// </summary>
    public void Insert(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database.
    /// </summary>
    public void Upsert(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts entities into the database.
    /// </summary>
    public void BulkInsert(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk upserts entities into the database.
    /// </summary>
    public void BulkUpsert(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database asynchronously.
    /// </summary>
    public Task InsertAsync(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts entities into the database asynchronously.
    /// </summary>
    public Task UpsertAsync(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts entities into the database in bulk.
    /// </summary>
    public Task BulkInsertAsync(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates entities into the database in bulk.
    /// </summary>
    public Task BulkUpsertAsync(DbConnection connection,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default);
}