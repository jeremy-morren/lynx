using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// A document store that disposes the underlying <see cref="DbContext"/>
/// </summary>
public interface IDocumentStoreDisposable : IDocumentStore, IDisposable, IAsyncDisposable;