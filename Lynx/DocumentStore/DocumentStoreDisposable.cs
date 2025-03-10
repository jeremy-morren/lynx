using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

internal class DocumentStoreDisposable<TContext> : DocumentStore<TContext>, IDocumentStoreDisposable
    where TContext : DbContext
{
    public DocumentStoreDisposable(TContext context,
        IEnumerable<IDocumentSessionListener>? listeners = null)
        : base(context, listeners)
    {
    }

    public void Dispose() => Context.Dispose();

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}