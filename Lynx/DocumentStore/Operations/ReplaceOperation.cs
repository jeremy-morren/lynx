using System.Data.Common;
using System.Linq.Expressions;
using Lynx.DocumentStore.Providers;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Replaces entities in the database that match the predicate with the provided entities (via bulk upsert).
/// </summary>
internal class ReplaceOperation<T> : OperationBase<T>, IDocumentSessionOperation
    where T : class
{
    private readonly IReadOnlyList<T> _entities;
    private readonly Expression<Func<T, bool>> _predicate;

    public ReplaceOperation(IReadOnlyList<T> entities, Expression<Func<T, bool>> predicate)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }
    
    public void SaveChanges(DbContext context, DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate
        context.Set<T>().Where(_predicate).ExecuteDelete();

        //Upsert the new entities
        GetService(context).Upsert(_entities, connection);
    }

    public async Task SaveChangesAsync(DbContext context, DbConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate
        await context.Set<T>().Where(_predicate).ExecuteDeleteAsync(cancellationToken);

        //Upsert the new entities
        await GetService(context).UpsertAsync(_entities, connection, cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}