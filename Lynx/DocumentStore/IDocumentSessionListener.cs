using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// Listener for document session, invoked after changes are saved.
/// </summary>
public interface IDocumentSessionListener
{
    /// <summary>
    /// Invoked after changes are saved with any entities that were inserted or updated.
    /// </summary>
    /// <param name="entities">Entities inserted or updated</param>
    /// <param name="context">Database context</param>
    void OnInsertedOrUpdated(IReadOnlyList<object> entities, DbContext context);

    internal void AfterCommit(IEnumerable<IDocumentSessionOperation> operations, DbContext context)
    {
        var insertedOrUpdated = operations.SelectMany(o => o.InsertedOrUpdatedDocuments).ToList();
        if (insertedOrUpdated.Count > 0)
            OnInsertedOrUpdated(insertedOrUpdated, context);
    }
}