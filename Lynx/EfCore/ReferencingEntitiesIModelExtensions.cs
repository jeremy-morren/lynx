using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

public static class ReferencingEntitiesIModelExtensions
{
    /// <summary>
    /// Gets all entities that reference the specified entity type (including deep references).
    /// </summary>
    /// <remarks>
    /// A referencing entity is an entity that can include the target entity in a query (including through a chain of references).
    /// </remarks>
    public static IReadOnlyList<IEntityType> GetReferencingEntities(this IModel model, Type entityType)
    {
        var key = (model, entityType);
        return Cache.GetOrAdd(key, GetReferencingEntities);
    }
    
    private static IReadOnlyList<IEntityType> GetReferencingEntities((IModel, Type) key) => 
        ReferencingEntities.GetReferencingEntities(key.Item1, key.Item2).ToArray();

    private static readonly ConcurrentDictionary<(IModel, Type), IReadOnlyList<IEntityType>> Cache = new();
}