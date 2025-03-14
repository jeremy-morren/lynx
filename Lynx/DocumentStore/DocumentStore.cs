using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lynx.DocumentStore;

internal class DocumentStore<TContext> : IDocumentStore where TContext : DbContext
{
    private readonly DocumentStoreOptions _options;
    private readonly List<IDocumentSessionListener> _listeners;

    public DocumentStore(TContext context,
        IOptions<DocumentStoreOptions> options,
        IEnumerable<IDocumentSessionListener>? listeners = null)
    {
        Context = context;

        _options = options.Value;
        _options.Validate();

        _listeners = listeners?.ToList() ?? [];
    }

    public DbContext Context { get; }

    public IDocumentSession CreateSession() =>
        new DocumentSession(Context, _options, _listeners);
}