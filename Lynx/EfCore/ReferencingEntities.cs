using System.Collections.Immutable;
using Lynx.EfCore.Helpers;
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

        return model.GetEntityTypes()
            .Where(t => HasReferencingEntities(t, target, []))
            .Where(t => !t.IsOwned())
            .Distinct();
    }

    /// <summary>
    /// Checks whether type references target.
    /// </summary>
    private static bool HasReferencingEntities(IEntityType type, IEntityType target, ImmutableHashSet<IEntityType> visited)
    {
        // Get all entities that reference target entity
        var navigations =
            from n in type.GetNavigations()
            where !visited.Contains(n.TargetEntityType) // Skip visited entities
            where (n.TargetEntityType == target && !n.ForeignKey.IsOwnership) //Check if this navigation references target
                || HasReferencingEntities(n.TargetEntityType, target, visited.Add(type)) //Or recurse
            select n.TargetEntityType;

        return navigations.Any();
    }
}