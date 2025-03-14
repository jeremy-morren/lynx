using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Providers.Common;

/// <summary>
/// A provider for Lynx to handle database operations.
/// </summary>
internal interface ILynxProvider
{
    /// <summary>
    /// Create a database service for the given entity type.
    /// </summary>
    ILynxDatabaseService<TEntity> CreateService<TEntity>(IModel model) where TEntity : class;
}