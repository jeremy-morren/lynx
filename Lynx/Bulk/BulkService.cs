using Lynx.DocumentStore.Providers;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.Bulk;

internal class BulkService : IBulkService
{
    public BulkService(DbContext context)
    {
        Context = context;
    }

    public DbContext Context { get; }

    private void EnsureTransactionStarted()
    {
        if (Context.Database.CurrentTransaction != null) return;
        
        const string error = $"Transaction must be started before bulk operations can be performed. Call {nameof(Context.Database)}.{nameof(Context.Database.BeginTransaction)}";
        throw new InvalidOperationException(error);
    }

    private ILynxEntityService<T> GetService<T>() where T : class
    {
        EnsureTransactionStarted();

        var provider = LynxProviderFactory.GetProvider(Context);
        return provider.GetService<T>();
    }

    public void BulkInsert<T>(IEnumerable<T> entities, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var service = GetService<T>();

        var connection = Context.Database.GetDbConnection();
        using var _ = OpenConnection.Open(connection);

        if (service is ILynxEntityServiceBulk<T> bulk)
            bulk.BulkInsert(entities, connection, cancellationToken);
        else
            service.Insert(entities, connection, cancellationToken);
    }

    public void BulkUpsert<T>(IEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var service = GetService<T>();

        var connection = Context.Database.GetDbConnection();
        using var _ = OpenConnection.Open(connection);

        if (useBulkOperation && service is ILynxEntityServiceBulk<T> bulk)
            bulk.BulkUpsert(entities, connection, cancellationToken);
        else
            service.Upsert(entities, connection, cancellationToken);
    }

    public async Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var service = GetService<T>();

        var connection = Context.Database.GetDbConnection();
        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);

        if (service is ILynxEntityServiceBulk<T> bulk)
            await bulk.BulkInsertAsync(entities, connection, cancellationToken);
        else
            await service.InsertAsync(entities, connection, cancellationToken);
    }

    public async Task BulkUpsertAsync<T>(IEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var service = GetService<T>();

        var connection = Context.Database.GetDbConnection();
        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);

        if (useBulkOperation && service is ILynxEntityServiceBulk<T> bulk)
            await bulk.BulkUpsertAsync(entities, connection, cancellationToken);
        else
            await service.UpsertAsync(entities, connection, cancellationToken);
    }

    public async Task BulkInsertAsync<T>(IAsyncEnumerable<T> entities, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var service = GetService<T>();

        var connection = Context.Database.GetDbConnection();
        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);

        if (service is ILynxEntityServiceBulk<T> bulk)
            await bulk.BulkInsertAsync(entities, connection, cancellationToken);
        else
            await service.InsertAsync(entities, connection, cancellationToken);
    }

    public async Task BulkUpsertAsync<T>(IAsyncEnumerable<T> entities, bool useBulkOperation, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var service = GetService<T>();

        var connection = Context.Database.GetDbConnection();
        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);

        if (useBulkOperation && service is ILynxEntityServiceBulk<T> bulk)
            await bulk.BulkUpsertAsync(entities, connection, cancellationToken);
        else
            await service.UpsertAsync(entities, connection, cancellationToken);
    }
}