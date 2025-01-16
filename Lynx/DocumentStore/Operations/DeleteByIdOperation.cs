using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

public class DeleteByIdOperation<T> : IDocumentSessionOperation where T : class
{
    private readonly object _id;

    public DeleteByIdOperation(object id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public void Execute(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Set<T>().FilterByKey(context, _id).ExecuteDelete();
    }

    public Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        return context.Set<T>().FilterByKey(context, _id).ExecuteDeleteAsync(cancellationToken);
    }

    public void AfterCommit(IDocumentSessionListener listener, DbContext context)
    {
        // Listener is not invoked for delete operations.
    }
}