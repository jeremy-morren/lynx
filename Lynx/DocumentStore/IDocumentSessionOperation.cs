using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// Represents a lynx database operation that will be applied to the database.
/// </summary>
public interface IDocumentSessionOperation
{
    /// <summary>
    /// Saves the changes to the database.
    /// </summary>
    internal void Execute(DbContext context);
    
    /// <summary>
    /// Saves the changes to the database asynchronously.
    /// </summary>
    internal Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Invoked after the transaction is committed if a listener is attached.
    /// </summary>
    internal void AfterCommit(IDocumentSessionListener listener, DbContext context);
}