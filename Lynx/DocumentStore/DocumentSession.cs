using System.Collections;
using System.Data;
using System.Linq.Expressions;
using Lynx.DocumentStore.Operations;
using Lynx.EfCore;
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

    public IReadOnlyList<IDocumentSessionOperation> Operations => _unitOfWork;
    
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


    private IQueryable<T> EnsureEntityType<T>() where T : class
    {
        //Will throw if the entity type is not found
        DbContext.Model.GetEntityType(typeof(T));

        return DbContext.Set<T>();
    }

    public void Store<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureEntityType<T>();

        if (entity is IEnumerable)
            throw new InvalidOperationException("Use Store(IEnumerable<T> entities) instead.");

        _unitOfWork.Add(new UpsertOperation<T>([entity]));
    }

    public void Store<T>(T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        _unitOfWork.Add(new UpsertOperation<T>(entities));
    }

    public void Store<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        var list = entities as IReadOnlyList<T> ?? entities.ToList();
        _unitOfWork.Add(new UpsertOperation<T>(list));
    }

    public void Insert<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureEntityType<T>();

        if (entity is IEnumerable)
            throw new InvalidOperationException("Use Insert(IEnumerable<T> entities) instead.");

        _unitOfWork.Add(new InsertOperation<T>([entity]));
    }

    public void Insert<T>(T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        _unitOfWork.Add(new InsertOperation<T>(entities));
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        var list = entities as IReadOnlyList<T> ?? entities.ToList();
        _unitOfWork.Add(new InsertOperation<T>(list));
    }

    public void Delete<T>(object id) where T : class
    {
        ArgumentNullException.ThrowIfNull(id);

        //Ensure the filter operation is valid
        EnsureEntityType<T>().FilterByKey(DbContext, id);
        _unitOfWork.Add(new DeleteByIdOperation<T>(id));
    }

    public void DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);

        EnsureEntityType<T>();
        _unitOfWork.Add(new DeleteWhereOperation<T>(predicate));
    }
    
    #endregion
}