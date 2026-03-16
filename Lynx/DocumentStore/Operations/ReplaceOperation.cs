using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Replaces entities in the database that match the predicate with the provided entities
/// </summary>
internal class ReplaceOperation<T> : OperationBase<T>, IDocumentSessionOperation
    where T : class
{
    private readonly IReadOnlyCollection<T> _entities;
    private readonly Expression<Func<T, bool>> _predicate;
    private readonly DocumentStoreOptions _options;

    public ReplaceOperation(
        IReadOnlyCollection<T> entities,
        Expression<Func<T, bool>> predicate,
        DocumentStoreOptions options)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _options = options;
    }
    
    public void SaveChanges(DbContext context, DbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate
        context.Set<T>().Where(_predicate).ExecuteDelete();

        //Insert the new entities
        var service = GetService(context);
        if (ShouldUseBulkInsert(_options, _entities.Count, service, out var bulk))
            bulk.BulkInsert(_entities, transaction);
        else
            service.Insert(_entities, transaction);
    }

    public async Task SaveChangesAsync(DbContext context, DbTransaction transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate
        await context.Set<T>().Where(_predicate).ExecuteDeleteAsync(cancellationToken);

        //Insert the new entities
        var service = GetService(context);
        if (ShouldUseBulkInsert(_options, _entities.Count, service, out var bulk))
            await bulk.BulkInsertAsync(_entities, transaction, cancellationToken);
        else
            await service.InsertAsync(_entities, transaction, cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}