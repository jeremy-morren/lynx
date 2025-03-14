using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using Lynx.DocumentStore.Operations;
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

    private readonly DocumentStoreOptions _options;
    private readonly List<IDocumentSessionListener> _listeners;
    
    public DocumentSession(
        DbContext context,
        DocumentStoreOptions options,
        List<IDocumentSessionListener> listeners)
    {
        _options = options;
        _listeners = listeners;

        DbContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<object> Operations => _unitOfWork;
    
    public DbContext DbContext { get; }
    
    #region Save Changes

    private Activity? StartSaveChangesActivity()
    {
        var activity = LynxTracing.ActivitySource.StartActivity(nameof(SaveChanges));
        activity?.AddTag("Context", DbContext.GetType());
        activity?.AddTag("Operations", _unitOfWork.Count);
        return activity;
    }

    public void SaveChanges()
    {
        using var activity = StartSaveChangesActivity();
        
        try
        {

            if (_unitOfWork.Count == 0)
                return; //Nothing to save

            var unitOfWork = _unitOfWork;

            DbContext.Database.CreateExecutionStrategy()
                .Execute(() =>
                {
                    using var conn = DocumentStoreConnection.OpenConnection(DbContext);
                    using var transaction = conn.BeginTransaction();
                    foreach (var o in unitOfWork)
                        o.SaveChanges(DbContext, conn);
                    transaction.Commit();
                });

            foreach (var listener in _listeners)
                listener.AfterCommit(unitOfWork, DbContext);
            _unitOfWork = [];
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(e);
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = StartSaveChangesActivity();

        try
        {
            if (_unitOfWork.Count == 0)
                return; //Nothing to save

            var unitOfWork = _unitOfWork;

            await DbContext.Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var conn = await DocumentStoreConnection.OpenConnectionAsync(DbContext, cancellationToken);
                    await using var transaction = await conn.BeginTransactionAsync(cancellationToken);
                    foreach (var o in unitOfWork)
                        await o.SaveChangesAsync(DbContext, conn, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                });

            foreach (var listener in _listeners)
                listener.AfterCommit(unitOfWork, DbContext);

            // Reset the unit of work
            _unitOfWork = [];
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(e);
            throw;
        }
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
        _unitOfWork.Add(new UpsertOperation<T>([entity], _options));
    }

    public void Store<T>(T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        if (entities.Length > 0)
            _unitOfWork.Add(new UpsertOperation<T>(entities, _options));
    }

    public void Store<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        var list = entities as IReadOnlyCollection<T> ?? entities.ToList();

        if (list.Count > 0)
            _unitOfWork.Add(new UpsertOperation<T>(list, _options));
    }

    public void Insert<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IEnumerable)
            throw new InvalidOperationException("Use Insert(IEnumerable<T> entities) instead.");

        EnsureEntityType<T>();
        _unitOfWork.Add(new InsertOperation<T>([entity], _options));
    }

    public void Insert<T>(T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        if (entities.Length > 0)
            _unitOfWork.Add(new InsertOperation<T>(entities, _options));
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        EnsureEntityType<T>();

        var list = entities as IReadOnlyCollection<T> ?? entities.ToList();

        if (list.Count > 0)
            _unitOfWork.Add(new InsertOperation<T>(list, _options));
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

    public void Replace<T>(IEnumerable<T> entities, Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(predicate);

        EnsureEntityType<T>();

        var list = entities as IReadOnlyCollection<T> ?? entities.ToList();

        if (list.Count == 0)
        {
            //Nothing to replace. We can use DeleteWhere instead.
            DeleteWhere(predicate);
        }
        else
        {
            _unitOfWork.Add(new ReplaceOperation<T>(list, predicate, _options));
        }
    }

    #endregion
}