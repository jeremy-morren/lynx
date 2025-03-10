using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Represents an operation to upsert entities in bulk to the database.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class InsertOperation<T> : OperationBase<T>, IDocumentSessionOperation
    where T : class
{
    private readonly IReadOnlyCollection<T> _entities;

    public InsertOperation(IReadOnlyCollection<T> entities)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }
    
    public void SaveChanges(DbContext context, DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(context);
        GetService(context).Insert(_entities, connection);
    }

    public Task SaveChangesAsync(DbContext context, DbConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return GetService(context).InsertAsync(_entities, connection, cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}