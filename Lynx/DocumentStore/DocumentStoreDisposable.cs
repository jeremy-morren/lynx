using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lynx.DocumentStore;

internal class DocumentStoreDisposable<TContext> : DocumentStore<TContext>, IDocumentStoreDisposable
    where TContext : DbContext
{
    public DocumentStoreDisposable(
        TContext context,
        IOptions<DocumentStoreOptions> options,
        IEnumerable<IDocumentSessionListener>? listeners = null)
        : base(context, options, listeners)
    {
    }

    public void Dispose() => Context.Dispose();

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}