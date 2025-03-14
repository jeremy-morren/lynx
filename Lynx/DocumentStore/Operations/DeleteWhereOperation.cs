using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Operation to delete entities from the database.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class DeleteWhereOperation<T> : IDocumentSessionOperation
    where T : class
{
    private readonly Expression<Func<T, bool>> _predicate;

    public DeleteWhereOperation(Expression<Func<T, bool>> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public void SaveChanges(DbContext context, DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Set<T>().Where(_predicate).ExecuteDelete();
    }

    public Task SaveChangesAsync(DbContext context, DbConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Set<T>().Where(_predicate).ExecuteDeleteAsync(cancellationToken);
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => [];
}