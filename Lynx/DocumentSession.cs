using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Lynx;

/// <summary>
/// Lynx document store session.
/// </summary>
internal class DocumentSession : IDocumentSession
{
    private readonly UnitOfWork _unitOfWork = [];
    
    private readonly IsolationLevel? _isolationLevel;
    private readonly IDocumentSessionListener? _listener;
    
    public DocumentSession(
        DbContext context, 
        IsolationLevel? isolationLevel,
        IDocumentSessionListener? listener)
    {
        _isolationLevel = isolationLevel;
        _listener = listener;
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
        _listener?.AfterCommit(_unitOfWork, DbContext);
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
        _listener?.AfterCommit(_unitOfWork, DbContext);
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

    public void DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _unitOfWork.Add(new DeleteWhereOperation<T>(predicate));
    }
    
    #endregion
}