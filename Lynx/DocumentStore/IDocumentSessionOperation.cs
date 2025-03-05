using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.DocumentStore;

/// <summary>
/// Represents a lynx database operation that will be applied to the database.
/// </summary>
internal interface IDocumentSessionOperation
{
    /// <summary>
    /// Saves the changes to the database.
    /// </summary>
    void SaveChanges(DbContext context);
    
    /// <summary>
    /// Saves the changes to the database asynchronously.
    /// </summary>
    Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Documents that will be inserted or updated
    /// </summary>
    /// <remarks>
    /// Sent to listeners
    /// </remarks>
    IEnumerable<object> InsertedOrUpdatedDocuments { get; }
}