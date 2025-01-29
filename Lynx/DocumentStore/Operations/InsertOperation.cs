using System.Diagnostics;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Represents an operation to upsert entities in bulk to the database.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class InsertOperation<T> : IDocumentSessionOperation
    where T : class
{
    private readonly IReadOnlyList<T> _entities;

    public InsertOperation(IReadOnlyList<T> entities)
    {
        _entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }
    
    public void Execute(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.BulkInsert(_entities);
    }

    public Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.BulkInsertAsync(_entities, cancellationToken: cancellationToken);
    }

    public void AfterCommit(IDocumentSessionListener listener, DbContext context)
    {
        ArgumentNullException.ThrowIfNull(listener);
        listener.OnInsertedOrUpdated(_entities, context);
    }
}