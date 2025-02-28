using System.Diagnostics;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Represents an operation to upsert entities in bulk to the database.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class UpsertOperation<T> : IDocumentSessionOperation
    where T : class
{
    private readonly IReadOnlyList<T> _entities;

    public UpsertOperation(IReadOnlyList<T> entities)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }
    
    public void Execute(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Debug.Assert(context.Database.CurrentTransaction != null);
        context.BulkInsertOrUpdate(_entities, BulkOptions.Config);
    }

    public Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        Debug.Assert(context.Database.CurrentTransaction != null);
        return context.BulkInsertOrUpdateAsync(_entities, BulkOptions.Config, cancellationToken: cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => _entities;
}