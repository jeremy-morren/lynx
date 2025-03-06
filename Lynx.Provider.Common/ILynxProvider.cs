using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common;

/// <summary>
/// A provider for Lynx to handle database operations.
/// </summary>
internal interface ILynxProvider
{
    /// <summary>
    /// Create a database service for the given entity type.
    /// </summary>
    static abstract ILynxDatabaseService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class;
}