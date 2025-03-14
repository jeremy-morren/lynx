using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lynx.DocumentStore;

internal class DocumentStoreFactory<TContext> : IDocumentStoreFactory
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _factory;
    private readonly IOptions<DocumentStoreOptions> _options;

    public DocumentStoreFactory(
        IDbContextFactory<TContext> factory,
        IOptions<DocumentStoreOptions> options)
    {
        _factory = factory;
        _options = options;
    }

    public IDocumentStoreDisposable CreateDocumentStore() =>
        new DocumentStoreDisposable<TContext>(_factory.CreateDbContext(), _options);

    public async Task<IDocumentStoreDisposable> CreateDocumentStoreAsync(CancellationToken cancellationToken = default)
    {
        var context = await _factory.CreateDbContextAsync(cancellationToken);
        return new DocumentStoreDisposable<TContext>(context, _options);
    }
}