using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

internal static class ReferencingEntities
{
    /// <summary>
    /// Gets all entities that reference the specified entity type (including deep references).
    /// </summary>
    /// <remarks>
    /// A referencing entity is an entity that can include the target entity in a query (including through a chain of references).
    /// </remarks>
    internal static IEnumerable<IEntityType> GetReferencingEntities(IModel model, Type entityType)
    {
        var target = model.GetEntityType(entityType);
        // Recurse
        return GetReferencingEntitiesInternal(model, target, [target]).Distinct();
    }

    private static IEnumerable<IEntityType> GetReferencingEntitiesInternal(
        IModel model, IEntityType target, HashSet<IEntityType> visited)
    {
        // Get all entities that reference target entity
        foreach (var type in model.GetEntityTypes())
        {
            if (visited.Contains(type))
                continue; // Skip target and visited entities

            if (!HasReferencingEntities(type, target))
                continue;

            visited.Add(type);
            
            // This entity references target.
            // Return type, and recursively get all entities that reference this entity.
            yield return type;

            foreach (var child in GetReferencingEntitiesInternal(model, type, visited))
                yield return child;
        }
    }

    /// <summary>
    /// Checks whether type references target entity.
    /// </summary>
    private static bool HasReferencingEntities(IEntityType type, IEntityType target)
    {
        // Get all entities that reference target entity
        var navigations =
            from n in type.GetNavigations()
            where n.TargetEntityType == target && !n.ForeignKey.IsOwnership
            select n.TargetEntityType;

        return navigations.Any();
    }
}