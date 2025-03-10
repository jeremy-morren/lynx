using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Inserts entities using bulk insert.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class BulkInsertOperation<T> : OperationBase<T>, IDocumentSessionOperation where T : class
{
    private readonly IReadOnlyCollection<T> _entities;

    public BulkInsertOperation(IReadOnlyCollection<T> entities)
    {
        _entities = entities;
    }
    
    public void SaveChanges(DbContext context, DbConnection connection)
    {
        GetService(context).BulkInsert(_entities, connection);
    }

    public Task SaveChangesAsync(DbContext context, DbConnection connection, CancellationToken cancellationToken)
    {
        return GetService(context).BulkInsertAsync(_entities, connection, cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}