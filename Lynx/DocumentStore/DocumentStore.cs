using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

internal class DocumentStore<TContext> : IDocumentStore where TContext : DbContext
{
    private readonly List<IDocumentSessionListener> _listeners;

    public DocumentStore(TContext context, IEnumerable<IDocumentSessionListener>? listeners = null)
    {
        Context = context;

        _listeners = listeners?.ToList() ?? [];
    }

    public DbContext Context { get; }

    public IDocumentSession OpenSession(IsolationLevel? isolationLevel = null) =>
        new DocumentSession(Context, isolationLevel, _listeners);
}