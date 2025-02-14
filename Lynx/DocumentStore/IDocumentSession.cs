using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// Lynx document store session.
/// </summary>
[PublicAPI]
public interface IDocumentSession
{
    #region Unit of Work
    
    /// <summary>
    /// Underlying database context for the session.
    /// </summary>
    DbContext DbContext { get; }
    
    /// <summary>
    /// Operations to be applied to the database when <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    IReadOnlyList<object> Operations { get; }
    
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
    /// Upserts the entity to the database using bulk upsert.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    void Store<T>(T entity) where T : class;

    /// <summary>
    /// Upserts the entities to the database using bulk upsert.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Store<T>(params T[] entities) where T : class;

    /// <summary>
    /// Upserts the entities to the database using bulk upsert.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Store<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Inserts the entity to the database using bulk upsert.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    void Insert<T>(T entity) where T : class;

    /// <summary>
    /// Inserts the entities to the database using bulk upsert.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Insert<T>(params T[] entities) where T : class;

    /// <summary>
    /// Inserts the entities to the database using bulk upsert.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    void Insert<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Deletes the entity with the specified id from the database.
    /// </summary>
    /// <param name="id">Id to delete</param>
    /// <typeparam name="T"></typeparam>
    void Delete<T>(object id) where T : class;

    /// <summary>
    /// Deletes entities from the database that match the predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    void DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class;

    /// <summary>
    /// Upserts the entity in the database using the default EF operations.
    /// </summary>
    void StoreViaContext<T>(T entity) where T : class;

    /// <summary>
    /// Replaces entities in the database that match the predicate with the provided entities (via bulk upsert).
    /// </summary>
    /// <param name="entities">Entities to upsert</param>
    /// <param name="predicate">Predicate to match entities to be replaced</param>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// <para>
    /// This method is useful for replacing entities in the database that match a certain condition with a new set of entities.
    /// It is the equivalent of deleting entities that match the predicate and then inserting the provided entities.
    /// </para>
    /// <para>
    /// The main difference is that this method will exclude the entities that are not being replaced from the delete operation (via ID),
    /// which avoids foreign key constraint issues.
    /// </para>
    /// </remarks>
    void Replace<T>(IEnumerable<T> entities, Expression<Func<T, bool>> predicate) where T : class;
    
    #endregion
}