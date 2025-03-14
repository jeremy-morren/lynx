using System.Diagnostics.CodeAnalysis;
using Lynx.DocumentStore.Providers;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

internal class OperationBase<T> where T : class
{
    protected static ILynxDatabaseService<T> GetService(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var provider = LynxProviderFactory.GetProvider(context);
        return provider.GetService<T>();
    }

    /// <summary>
    /// Returns whether bulk insert operations should be used for the given entity count and service.
    /// </summary>
    protected static bool ShouldUseBulkInsert(
        DocumentStoreOptions options,
        int entityCount,
        ILynxDatabaseService<T> service,
        [MaybeNullWhen(false)] out ILynxDatabaseServiceBulk<T> bulkService)
    {
        if (entityCount >= options.BulkOperationThreshold &&
            service is ILynxDatabaseServiceBulk<T> bulk)
        {
            bulkService = bulk;
            return true;
        }
        bulkService = null;
        return false;
    }

    /// <summary>
    /// Returns whether bulk upsert operations should be used for the given entity count and service.
    /// </summary>
    protected static bool ShouldUseBulkUpsert(
        DocumentStoreOptions options,
        int entityCount,
        ILynxDatabaseService<T> service,
        [MaybeNullWhen(false)] out ILynxDatabaseServiceBulk<T> bulkService)
    {
        if (options.UseBulkOperationsForUpsert)
            return ShouldUseBulkInsert(options, entityCount, service, out bulkService);
        bulkService = null;
        return false;
    }

}