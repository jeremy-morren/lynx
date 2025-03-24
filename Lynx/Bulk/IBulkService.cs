using JetBrains.Annotations;
using Lynx.DocumentStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.Bulk;

/// <summary>
/// A service for performing bulk operations on an EF core context manually.
/// </summary>
[PublicAPI]
public interface IBulkService
{
    /// <summary>
    /// Underlying DB context
    /// </summary>
    DbContext Context { get; }

    /// <summary>
    /// Inserts data to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    void BulkInsert<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Upserts to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="useBulkOperation">Whether to use specialized bulk operation if the provider supports it.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    void BulkUpsert<T>(IEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Inserts data to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Upserts to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="useBulkOperation">Whether to use specialized bulk operation if the provider supports it.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    Task BulkUpsertAsync<T>(IEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Inserts data to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    Task BulkInsertAsync<T>(IAsyncEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Upserts to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="useBulkOperation">Whether to use specialized bulk operation if the provider supports it.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    Task BulkUpsertAsync<T>(IAsyncEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken = default) where T : class;

    #region Overloads

    /// <summary>
    /// Upserts to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    void BulkUpsert<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        => BulkUpsert(entities, false, cancellationToken);

    /// <summary>
    /// Upserts to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    Task BulkUpsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        => BulkUpsertAsync(entities, false, cancellationToken);

    /// <summary>
    /// Upserts to underlying database in bulk
    /// </summary>
    /// <param name="entities">Data to write</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Entity type</typeparam>
    /// <remarks>
    /// <para>
    /// Note that this method must be called after <see cref="DatabaseFacade.BeginTransaction"/>
    /// </para>
    /// </remarks>
    Task BulkUpsertAsync<T>(IAsyncEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        => BulkUpsertAsync(entities, false, cancellationToken);

    #endregion
}