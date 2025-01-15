using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Lynx.DocumentStore.Operations;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// Lynx document store session.
/// </summary>
internal class DocumentSession : IDocumentSession
{
    private readonly UnitOfWork _unitOfWork = [];
    
    private readonly IsolationLevel? _isolationLevel;
    private readonly List<IDocumentSessionListener> _listeners;
    
    public DocumentSession(
        DbContext context, 
        IsolationLevel? isolationLevel,
        List<IDocumentSessionListener> listeners)
    {
        _isolationLevel = isolationLevel;
        _listeners = listeners;

        DbContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<IDocumentSessionOperations> Operations => _unitOfWork;
    
    public DbContext DbContext { get; }
    
    #region Save Changes

    public void SaveChanges()
    {
        using var transaction = _isolationLevel.HasValue 
            ? DbContext.Database.BeginTransaction(_isolationLevel.Value)
            : DbContext.Database.BeginTransaction();
        foreach (var o in _unitOfWork)
            o.Execute(DbContext);
        transaction.Commit();

        foreach (var listener in _listeners)
            listener.AfterCommit(_unitOfWork, DbContext);
        _unitOfWork.Reset();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = _isolationLevel.HasValue 
            ? await DbContext.Database.BeginTransactionAsync(_isolationLevel.Value, cancellationToken)
            : await DbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var o in _unitOfWork)
            await o.SaveChangesAsync(DbContext, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        foreach (var listener in _listeners)
            listener.AfterCommit(_unitOfWork, DbContext);
        _unitOfWork.Reset();
    }
    
    #endregion
    
    #region Operations

    public void Store<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        if (entity is IEnumerable)
            throw new InvalidOperationException("Use Store(IEnumerable<T> entities) instead.");
        
        _unitOfWork.Add(new UpsertOperation<T>([entity]));
    }

    public void Store<T>(params T[] entities) where T : class
    {
        _unitOfWork.Add(new UpsertOperation<T>(entities));
    }

    public void Store<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        var list = entities as IReadOnlyList<T> ?? entities.ToList();
        _unitOfWork.Add(new UpsertOperation<T>(list));
    }

    public void Insert<T>(T entity) where T : class
    {
        Store<T>(entity); //TODO: Implement Insert
    }

    public void Insert<T>(params T[] entities) where T : class
    {
        Store<T>(entities); //TODO: Implement Insert
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        Store<T>(entities); //TODO: Implement Insert
    }

    public void DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _unitOfWork.Add(new DeleteWhereOperation<T>(predicate));
    }
    
    #endregion
}