using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

internal class DocumentStoreFactory<TContext> : IDocumentStoreFactory
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _factory;

    public DocumentStoreFactory(IDbContextFactory<TContext> factory)
    {
        _factory = factory;
    }

    public IDocumentStoreDisposable CreateDocumentStore() => new DocumentStoreDisposable<TContext>(_factory.CreateDbContext());

    public async Task<IDocumentStoreDisposable> CreateDocumentStoreAsync(CancellationToken cancellationToken = default)
    {
        var context = await _factory.CreateDbContextAsync(cancellationToken);
        return new DocumentStoreDisposable<TContext>(context);
    }
}