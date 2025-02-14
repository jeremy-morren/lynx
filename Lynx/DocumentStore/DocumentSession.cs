using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Linq.Expressions;
using Lynx.DocumentStore.Operations;
using Lynx.EfCore;
using Lynx.EfCore.Helpers;
using Lynx.EfCore.KeyFilter;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// Lynx document store session.
/// </summary>
internal class DocumentSession : IDocumentSession
{
    private UnitOfWork _unitOfWork = [];

    private readonly List<IDocumentSessionListener> _listeners;
    
    public DocumentSession(DbContext context, List<IDocumentSessionListener> listeners)
    {
        _listeners = listeners;

        DbContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<object> Operations => _unitOfWork;
    
    public DbContext DbContext { get; }
    
    #region Save Changes

    public void SaveChanges()
    {
        if (_unitOfWork.Count == 0)
            return; //Nothing to save

        var unitOfWork = _unitOfWork;

        DbContext.Database.CreateExecutionStrategy().Execute(() =>
        {
            foreach (var o in unitOfWork)
                o.Execute(DbContext);
        });

        foreach (var listener in _listeners)
            listener.AfterCommit(unitOfWork, DbContext);
        _unitOfWork = [];
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_unitOfWork.Count == 0)
            return; //Nothing to save

        var unitOfWork = _unitOfWork;

        await DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            foreach (var o in unitOfWork)
                await o.SaveChangesAsync(DbContext, cancellationToken);
        });

        foreach (var listener in _listeners)
            listener.AfterCommit(unitOfWork, DbContext);

        // Reset the unit of work
        _unitOfWork = [];
    }
    
    #endregion
    
    #region Operations

    private DbSet<T> EnsureEntityType<T>() where T : class
    {
        //Will throw if the entity type is not found
        DbContext.Model.GetEntityType(typeof(T));

        return DbContext.Set<T>();
    }

    public void Store<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IEnumerable)
            throw new InvalidOperationException("Use Store(IEnumerable<T> entities) instead.");

        EnsureEntityType<T>();
        _unitOfWork.Add(new UpsertOperation<T>([entity]));
    }

    public void Store<T>(T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        if (entities.Length > 0)
            _unitOfWork.Add(new UpsertOperation<T>(entities));
    }

    public void Store<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        var list = entities as IReadOnlyList<T> ?? entities.ToList();

        if (list.Count > 0)
            _unitOfWork.Add(new UpsertOperation<T>(list));
    }

    public void Insert<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IEnumerable)
            throw new InvalidOperationException("Use Insert(IEnumerable<T> entities) instead.");

        EnsureEntityType<T>();
        _unitOfWork.Add(new InsertOperation<T>([entity]));
    }

    public void Insert<T>(T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        if (entities.Length > 0)
            _unitOfWork.Add(new InsertOperation<T>(entities));
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        var list = entities as IReadOnlyList<T> ?? entities.ToList();

        if (list.Count > 0)
            _unitOfWork.Add(new InsertOperation<T>(list));
    }

    public void Delete<T>(object id) where T : class
    {
        ArgumentNullException.ThrowIfNull(id);

        //Ensure the filter operation is valid
        EnsureEntityType<T>().FilterByKey(id);
        _unitOfWork.Add(new DeleteByIdOperation<T>(id));
    }

    public void DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);

        EnsureEntityType<T>();
        _unitOfWork.Add(new DeleteWhereOperation<T>(predicate));
    }

    public void StoreViaContext<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        EnsureEntityType<T>();
        _unitOfWork.Add(new UpsertViaEFOperation<T>(entity));
    }

    public void Replace<T>(IEnumerable<T> entities, Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(predicate);

        EnsureEntityType<T>();

        var list = entities as IReadOnlyList<T> ?? entities.ToList();

        if (list.Count == 0)
        {
            //Nothing to replace. We can use DeleteWhere instead.
            DeleteWhere(predicate);
        }
        else
        {
            _unitOfWork.Add(new ReplaceOperation<T>(list, predicate, DbContext.Model));
        }
    }

    #endregion
}