using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore.Chains;

public static class EntityChainModelExtensions
{
    /// <summary>
    /// Gets all entities with a foreign key to the specified entity type
    /// </summary>
    /// <remarks>
    /// Returns all entities that have a foreign key to the current entity type
    /// (or to an entity with a foreign key to the current entity type, recursively)
    /// </remarks>
    public static IReadOnlyList<EntityChain<IForeignKey>> GetReferencingForeignKeys(this IModel model, Type entityType) =>
        ForeignKeyCache.GetOrAdd((model, entityType), static x => x.Item1.GetForeignKeyChains(x.Item2));

    private static readonly ConcurrentDictionary<(IModel, Type), IReadOnlyList<EntityChain<IForeignKey>>> ForeignKeyCache = new();

    /// <summary>
    /// Gets all entities with a navigation to the specified entity type
    /// </summary>
    /// <remarks>
    /// Returns all entities that have a navigation to the current entity type
    /// (or to an entity with a navigation to the current entity type, recursively)
    /// </remarks>
    public static IReadOnlyList<EntityChain<INavigation>> GetReferencingNavigations(this IModel model, Type entityType) =>
        NavigationCache.GetOrAdd((model, entityType), static x => x.Item1.GetNavigationChains(x.Item2));

    private static readonly ConcurrentDictionary<(IModel, Type), IReadOnlyList<EntityChain<INavigation>>> NavigationCache = new();
}