using Microsoft.EntityFrameworkCore;

namespace Lynx;

/// <summary>
/// Listener for document session, invoked after changes are saved.
/// </summary>
public interface IDocumentSessionListener
{
    /// <summary>
    /// Invoked after changes are saved with the entities that were upserted and the database context.
    /// </summary>
    /// <param name="entities">Entities inserted or updated</param>
    /// <param name="context">Database context</param>
    void OnUpserted(IReadOnlyList<object> entities, DbContext context);

    /// <summary>
    /// Invoked after changes are saved with the entities that were upserted and the database context.
    /// </summary>
    /// <param name="entities">Entities inserted or updated</param>
    void OnUpserted(IReadOnlyList<object> entities);

    internal void AfterCommit(IEnumerable<IDocumentSessionOperations> operations, DbContext context)
    {
        foreach (var o in operations)
            o.AfterCommit(this, context);
    }
}