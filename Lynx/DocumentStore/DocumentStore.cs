using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

internal class DocumentStore<TContext> : IDocumentStore where TContext : DbContext
{
    private readonly IDocumentSessionListener? _listener;

    public DocumentStore(DbContext context, IDocumentSessionListener? listener = null)
    {
        Context = context;
        _listener = listener;
    }

    public DbContext Context { get; }

    public IDocumentSession OpenSession(IsolationLevel? isolationLevel = null) =>
        new DocumentSession(Context, isolationLevel, _listener);
}