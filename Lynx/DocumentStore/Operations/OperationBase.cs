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
}