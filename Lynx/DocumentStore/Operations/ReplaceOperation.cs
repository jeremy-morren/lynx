using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Lynx.EfCore;
using Lynx.EfCore.KeyFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Replaces entities in the database that match the predicate with the provided entities (via bulk upsert).
/// </summary>
internal class ReplaceOperation<T> : IDocumentSessionOperation
    where T : class
{
    private readonly IReadOnlyList<T> _entities;
    private readonly Expression<Func<T, bool>> _predicate;

    public ReplaceOperation(IReadOnlyList<T> entities, Expression<Func<T, bool>> predicate)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }
    
    public void SaveChanges(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        //Delete entities that match the predicate
        context.Set<T>().Where(_predicate).ExecuteDelete();

        //Upsert the new entities
        context.BulkInsertOrUpdate(_entities, BulkOptions.Config);
    }

    public async Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            //Delete entities that match the predicate
            await context.Set<T>().Where(_predicate).ExecuteDeleteAsync(cancellationToken);

            //Upsert the new entities
            await context.BulkInsertOrUpdateAsync(_entities, BulkOptions.Config, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}