using System.Reflection;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

internal static class IncludeRelatedEntities
{
    /// <summary>
    /// Gets all properties that should be included for the entity type.
    /// </summary>
    public static IEnumerable<string> GetIncludeProperties(IModel model, Type entityType) =>
        GetIncludePropertiesInternal(model.GetEntityType(entityType), null);
    
    private static IEnumerable<string> GetIncludePropertiesInternal(IEntityType entityType, IForeignKey? parentKey) =>
        GetNavigations(entityType, parentKey)
            .SelectMany(nav =>
            {
                //Recursively get all children, except for this navigation
                var children = GetIncludePropertiesInternal(nav.TargetEntityType, nav.ForeignKey).Select(n => $"{nav.Name}.{n}");

                return nav.ForeignKey.IsOwnership
                    ? children // For owned properties, return only the children
                    : children.Prepend(nav.Name); // For non-owned properties, return the navigation itself and its children
            });

    /// <summary>
    /// Gets all navigations for the entity type, excluding the parent key.
    /// </summary>
    private static IEnumerable<INavigation> GetNavigations(IEntityType entity, IForeignKey? parentKey) =>
        from nav in entity.GetNavigations()
        // Exclude parent key (if properties on both sides are defined) to stop infinite recursion
        where nav.ForeignKey != parentKey &&
              // Exclude collections
              nav is { IsCollection: false } &&
              // Exclude navigations marked with LynxDoNotIncludeReferencedAttribute
              nav.PropertyInfo?.GetCustomAttribute<LynxDoNotIncludeReferencedAttribute>() == null
        select nav;
}