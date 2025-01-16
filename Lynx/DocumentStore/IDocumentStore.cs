using System.Data;
using JetBrains.Annotations;
using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// A document store that wraps a <see cref="DbContext"/>
/// </summary>
[PublicAPI]
public interface IDocumentStore
{
    /// <summary>
    /// The underlying <see cref="DbContext"/>
    /// </summary>
    DbContext Context { get; }

    /// <summary>
    /// Opens a new document session to write to the store
    /// </summary>
    /// <param name="isolationLevel">Transaction isolation level</param>
    IDocumentSession OpenSession(IsolationLevel? isolationLevel = null);
}