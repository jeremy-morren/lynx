using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// Lynx document store session.
/// </summary>
public interface IDocumentSession
{
    #region Unit of Work
    
    /// <summary>
    /// Returns the database context for the session.
    /// </summary>
    DbContext DbContext { get; }
    
    /// <summary>
    /// Operations to be applied to the database when <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    IReadOnlyList<IDocumentSessionOperations> Operations { get; }
    
    /// <summary>
    /// Saves the changes to the database as a single transaction.
    /// </summary>
    void SaveChanges();
    
    /// <summary>
    /// Saves the changes to the database as a single transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Operations
    
    /// <summary>
    /// Upserts the entity to the database.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    void Store<T>(T entity) where T : class;
    
    /// <summary>
    /// Upserts the entities to the database.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Store<T>(params T[] entities) where T : class;
    
    /// <summary>
    /// Upserts the entities to the database.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Store<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Inserts the entity to the database.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    void Insert<T>(T entity) where T : class;

    /// <summary>
    /// Inserts the entities to the database.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Insert<T>(params T[] entities) where T : class;

    /// <summary>
    /// Inserts the entities to the database.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Insert<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Deletes entities from the database that match the predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    void DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class;

    
    #endregion
}