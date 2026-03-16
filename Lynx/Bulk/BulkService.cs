using System.Data.Common;
using Lynx.DocumentStore.Providers;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.Bulk;

internal class BulkService : IBulkService
{
    public BulkService(DbContext context)
    {
        Context = context;
    }

    public DbContext Context { get; }

    private IDbContextTransaction GetTransaction()
    {
        const string error = $"Transaction must be started before bulk operations can be performed. Call {nameof(Context.Database)}.{nameof(Context.Database.BeginTransaction)}";
        return Context.Database.CurrentTransaction ?? throw new InvalidOperationException(error);
    }

    private ILynxEntityService<T> GetService<T>() where T : class
    {
        var provider = LynxProviderFactory.GetProvider(Context);
        return provider.GetService<T>();
    }

    private void Execute<T>(Action<DbTransaction, ILynxEntityService<T>> handler) where T : class
    {
        var service = GetService<T>();

        var transaction = GetTransaction();
        var dbTransaction = transaction.GetDbTransaction();
        using var _ = OpenConnection.Open(dbTransaction.Connection);
        handler(dbTransaction, service);
    }

    public void BulkInsert<T>(IEnumerable<T> entities, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        Execute<T>((dbTransaction, service) =>
        {
            if (service is ILynxEntityServiceBulk<T> bulk)
                bulk.BulkInsert(entities, dbTransaction, cancellationToken);
            else
                service.Insert(entities, dbTransaction, cancellationToken);
        });
    }

    public void BulkUpsert<T>(IEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        Execute<T>((dbTransaction, service) =>
        {
            if (useBulkOperation && service is ILynxEntityServiceBulk<T> bulk)
                bulk.BulkUpsert(entities, dbTransaction, cancellationToken);
            else
                service.Upsert(entities, dbTransaction, cancellationToken);
        });
    }

    private async Task ExecuteAsync<T>(Func<DbTransaction, ILynxEntityService<T>, Task> handler) where T : class
    {
        var service = GetService<T>();

        var transaction = GetTransaction();
        var dbTransaction = transaction.GetDbTransaction();
        await using var _ = OpenConnection.Open(dbTransaction.Connection);
        await handler(dbTransaction, service);
    }

    public Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        return ExecuteAsync<T>((dbTransaction, service) =>
            service is ILynxEntityServiceBulk<T> bulk
                ? bulk.BulkInsertAsync(entities, dbTransaction, cancellationToken)
                : service.InsertAsync(entities, dbTransaction, cancellationToken));
    }

    public Task BulkUpsertAsync<T>(IEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        return ExecuteAsync<T>((dbTransaction, service) =>
            service is ILynxEntityServiceBulk<T> bulk
                ? bulk.BulkUpsertAsync(entities, dbTransaction, cancellationToken)
                : service.UpsertAsync(entities, dbTransaction, cancellationToken));
    }

    public Task BulkInsertAsync<T>(IAsyncEnumerable<T> entities, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        return ExecuteAsync<T>((dbTransaction, service) =>
            service is ILynxEntityServiceBulk<T> bulk
                ? bulk.BulkInsertAsync(entities, dbTransaction, cancellationToken)
                : service.InsertAsync(entities, dbTransaction, cancellationToken));
    }

    public Task BulkUpsertAsync<T>(IAsyncEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        return ExecuteAsync<T>((dbTransaction, service) =>
            service is ILynxEntityServiceBulk<T> bulk
                ? bulk.BulkUpsertAsync(entities, dbTransaction, cancellationToken)
                : service.UpsertAsync(entities, dbTransaction, cancellationToken));
    }
}