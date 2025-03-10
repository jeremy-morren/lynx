﻿using JetBrains.Annotations;
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
    IDocumentSession OpenSession();
}