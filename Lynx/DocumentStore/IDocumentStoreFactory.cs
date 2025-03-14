using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// A document store factory that wraps a <see cref="IDbContextFactory{TContext}"/>
/// </summary>
public interface IDocumentStoreFactory
{
    /// <summary>
    /// Create a new <see cref="IDocumentStore"/>
    /// </summary>
    IDocumentStoreDisposable CreateDocumentStore();

    /// <summary>
    /// Create a new <see cref="IDocumentStore"/> asynchronously
    /// </summary>
    Task<IDocumentStoreDisposable> CreateDocumentStoreAsync(CancellationToken cancellationToken = default);
}