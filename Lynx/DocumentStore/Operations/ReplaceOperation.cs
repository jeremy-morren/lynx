using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Lynx.EfCore;
using Lynx.EfCore.KeyFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Replaces entities in the database that match the predicate with the provided entities (via bulk upsert).
/// </summary>
internal class ReplaceOperation<T> : IDocumentSessionOperation
    where T : class
{
    private readonly List<object> _ids;
    private readonly IReadOnlyList<T> _entities;
    private readonly Expression<Func<T, bool>> _predicate;

    public ReplaceOperation(IReadOnlyList<T> entities, Expression<Func<T, bool>> predicate, IModel model)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        if (entities.Count == 0)
            throw new ArgumentException("Entities list must not be empty.", nameof(entities));

        //Get the Ids here, so that any errors are thrown when Insert is called (rather than when SaveChanges is called)
        _ids = entities.Select(model.GetEntityKey).ToList();
    }
    
    public void Execute(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate, excluding the new entities
        var predicate = CombinePredicate(context, _predicate, _ids);
        context.Set<T>().Where(predicate).ExecuteDelete();

        //Upsert the new entities
        context.BulkInsertOrUpdate(_entities);
    }

    public async Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate, excluding the new entities
        var predicate = CombinePredicate(context, _predicate, _ids);
        await context.Set<T>().Where(predicate).ExecuteDeleteAsync(cancellationToken);

        //Upsert the new entities
        await context.BulkInsertOrUpdateAsync(_entities, cancellationToken: cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;

    private static Expression<Func<T, bool>> CombinePredicate(DbContext context, Expression<Func<T, bool>> predicate, List<object> ids)
    {
        //Extract the parameter from the predicate
        var parameter = predicate.Parameters[0];
        var idFilter = EntityKeyFilterHelpers.GetMultipleKeyPredicate<T>(ids, parameter, context);

        //Combine the predicates: predicate && !idFilter
        var combined = Expression.AndAlso(predicate.Body, Expression.Not(idFilter));
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}