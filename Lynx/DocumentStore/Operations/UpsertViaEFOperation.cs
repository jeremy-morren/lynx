using Lynx.EfCore;
using Lynx.EfCore.KeyFilter;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore.Operations;

/// <summary>
/// Upsert a document via EF (i.e. using default EF operations)
/// </summary>
internal class UpsertViaEFOperation<T> : IDocumentSessionOperation where T : class
{
    private readonly T _document;

    public UpsertViaEFOperation(T document)
    {
        _document = document;
    }

    public void SaveChanges(DbContext context)
    {
        var id = context.Model.GetEntityKey(_document);
        context.Set<T>().FilterByKey(id).ExecuteDelete();

        context.Set<T>().Add(_document);
        context.SaveChanges();

        context.ChangeTracker.Clear();
    }

    public async Task SaveChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var id = context.Model.GetEntityKey(_document);
        await context.Set<T>().FilterByKey(id).ExecuteDeleteAsync(cancellationToken);

        context.Set<T>().Add(_document);
        await context.SaveChangesAsync(cancellationToken);

        context.ChangeTracker.Clear();
    }

    public IEnumerable<object> InsertedOrUpdatedDocuments => [_document];
}