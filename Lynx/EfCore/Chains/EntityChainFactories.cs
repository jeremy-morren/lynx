using System.Collections.Immutable;
using System.Diagnostics;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore.Chains;

internal static class EntityChainFactories
{
    #region Foreign Keys

    internal static IReadOnlyList<EntityChain<IForeignKey>> GetForeignKeyChains(this IModel model, Type entityType)
    {
        var target = model.GetEntityType(entityType);
        var result = CreateForeignKeyChains(target, [])
            .Select(x => new EntityChain<IForeignKey>(x.Principal, target, x.Chain))
            .ToArray();
        return result;
    }

    /// <summary>
    /// Checks whether type references target.
    /// </summary>
    private static IEnumerable<(IEntityType Principal, ImmutableList<IForeignKey> Chain)> CreateForeignKeyChains(
        IEntityType target, ImmutableHashSet<IEntityType> visited)
    {
        // Get all foreign keys that target the entity
        foreach (var foreignKey in target.GetReferencingForeignKeys())
        {
            if (foreignKey.IsOwnership)
            {
                // Ignore ownership foreign keys
                continue;
            }

            var principal = foreignKey.DeclaringEntityType;

            var chain = ImmutableList.Create(foreignKey);

            // If declaring entity is an owned entity, move up to root
            while (principal.FindOwnership() is { } toOwner)
            {
                principal = toOwner.PrincipalEntityType;
                chain = chain.Insert(0, toOwner);
            }

            // We now have a root->target chain, yield
            yield return (principal, chain);

            if (visited.Contains(principal))
                continue;

            foreach (var super in CreateForeignKeyChains(principal, visited.Add(principal)))
            {
                // For each chain that points to principal, insert our chain at the end and yield
                yield return (super.Principal, super.Chain.AddRange(chain));
            }
        }
    }

    #endregion

    #region Navigations

    internal static IReadOnlyList<EntityChain<INavigation>> GetNavigationChains(this IModel model, Type entityType)
    {
        var target = model.GetEntityType(entityType);
        var result = CreateNavigationChains(target, [])
            .Select(x => new EntityChain<INavigation>(x.Principal, target, x.Chain))
            .ToArray();
        return result;
    }

    /// <summary>
    /// Checks whether type references target.
    /// </summary>
    private static IEnumerable<(IEntityType Principal, ImmutableList<INavigation> Chain)> CreateNavigationChains(
        IEntityType target, ImmutableHashSet<IEntityType> visited)
    {
        // Get all foreign keys that target the entity
        foreach (var navigation in target.GetReferencingNavigations())
        {
            var declaring = navigation.DeclaringEntityType;

            var chain = ImmutableList.Create(navigation);

            // If declaring entity is an owned entity, move up to root

            while (declaring.FindOwnership() is { } toOwner)
            {
                declaring = toOwner.PrincipalEntityType;
                var toOwned = toOwner.PrincipalToDependent;
                Debug.Assert(toOwned != null);
                chain = chain.Insert(0, toOwned);
            }

            // We now have a root->target chain, yield
            yield return (declaring, chain);

            if (visited.Contains(declaring))
                continue;

            foreach (var super in CreateNavigationChains(declaring, visited.Add(declaring)))
            {
                // For each chain that points to principal, insert our chain at the end and yield
                yield return (super.Principal, super.Chain.AddRange(chain));
            }
        }
    }

    private static IEnumerable<INavigation> GetReferencingNavigations(this IEntityType entityType)
    {
        if (entityType.IsOwned())
        {
            // Owned entity, only owner can reference
            return entityType.GetNavigations();
        }

        // Non-owned entity, search all navigations in model
        return entityType.Model.GetEntityTypes()
            .SelectMany(t => t.GetNavigations())
            // Where the foreign key target is entityType
            .Where(t => t.TargetEntityType == entityType)
            .Distinct();
    }

    #endregion
}