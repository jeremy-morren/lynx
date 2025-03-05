using Lynx.EfCore.KeyFilter;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

public class DeleteByIdOperation<T> : IDocumentSessionOperation where T : class
{
    private readonly object _id;

    public DeleteByIdOperation(object id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public void SaveChanges(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Set<T>().FilterByKey(_id).ExecuteDelete();
    }

    public Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        return context.Set<T>().FilterByKey(_id).ExecuteDeleteAsync(cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => [];
}