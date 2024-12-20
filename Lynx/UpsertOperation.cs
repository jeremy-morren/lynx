﻿using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Lynx;

/// <summary>
/// Represents an operation to upsert entities to the database.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class UpsertOperation<T> : IDocumentSessionOperations
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
        context.BulkInsertOrUpdate(_entities);
    }

    public Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.BulkInsertOrUpdateAsync(_entities, cancellationToken: cancellationToken);
    }

    public void AfterCommit(IDocumentSessionListener listener, DbContext context)
    {
        ArgumentNullException.ThrowIfNull(listener);
        listener.OnUpserted(_entities);
        listener.OnUpserted(_entities, context);
    }
}