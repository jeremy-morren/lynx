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
    private readonly DocumentStoreOptions _options;

    public InsertOperation(IReadOnlyCollection<T> entities, DocumentStoreOptions options)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _options = options;
    }

    public void SaveChanges(DbContext context, DbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(context);

        var service = GetService(context);
        if (ShouldUseBulkInsert(_options, _entities.Count, service, out var bulk))
            bulk.BulkInsert(_entities, transaction);
        else
            service.Insert(_entities, transaction);
    }

    public Task SaveChangesAsync(DbContext context, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var service = GetService(context);
        return ShouldUseBulkInsert(_options, _entities.Count, service, out var bulk)
            ? bulk.BulkInsertAsync(_entities, transaction, cancellationToken)
            : service.InsertAsync(_entities, transaction, cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}