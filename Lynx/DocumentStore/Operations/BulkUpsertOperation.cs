using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Upserts entities using bulk upsert.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class BulkUpsertOperation<T> : OperationBase<T>, IDocumentSessionOperation where T : class
{
    private readonly IReadOnlyCollection<T> _entities;

    public BulkUpsertOperation(IReadOnlyCollection<T> entities)
    {
        _entities = entities;
    }
    
    public void SaveChanges(DbContext context, DbConnection connection)
    {
        GetService(context).BulkUpsert(_entities, connection);
    }

    public Task SaveChangesAsync(DbContext context, DbConnection connection, CancellationToken cancellationToken)
    {
        return GetService(context).BulkUpsertAsync(_entities, connection, cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}