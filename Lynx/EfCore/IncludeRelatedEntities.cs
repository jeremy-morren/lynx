using System.Diagnostics;
using System.Reflection;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

internal static class IncludeRelatedEntities
{
    /// <summary>
    /// Gets all properties that should be included for the entity type
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="entityType">Entity type to get include properties for</param>
    public static IEnumerable<string> GetIncludeProperties(IModel model, Type entityType) =>
        GetIncludePropertiesInternal(model.GetEntityType(entityType), null);

    /// <summary>
    /// Gets all properties that should be included for the property chain, excluding cyclic references
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="rootEntityType">The root entity type (i.e. the entity type being queried)</param>
    /// <param name="propertyChain">Property chain that specifies include path</param>
    public static IEnumerable<string> GetIncludeProperties(IModel model, Type rootEntityType, IReadOnlyList<PropertyInfo> propertyChain)
    {
        var navigation = GetNavigationChain(model, rootEntityType, propertyChain).Last();
        return GetIncludePropertiesInternal(navigation.TargetEntityType, navigation.ForeignKey);
    }

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

    private static IEnumerable<INavigation> GetNavigationChain(IModel model, Type rootEntityType, IReadOnlyList<PropertyInfo> properties)
    {
        if (properties.Count == 0)
            throw new ArgumentOutOfRangeException(nameof(properties), "At least one property must be provided");

        var root = properties[0];
        Debug.Assert(root.DeclaringType != null, "Property has no declaring type");
        Debug.Assert(rootEntityType.IsAssignableTo(root.DeclaringType), "Root property is not declared on root entity type");
        var entity = model.GetEntityType(rootEntityType);

        foreach (var property in properties)
        {
            var navigation = entity.GetNavigations()
                .SingleOrDefault(n => n.PropertyInfo != null && n.PropertyInfo.HasSameMetadataDefinitionAs(property));
            if (navigation == null)
            {
                // Property has not been mapped as a navigation, fail
                throw new InvalidOperationException($"Property {property.Name} is not a navigation on {entity}");
            }
            yield return navigation;
            entity = navigation.TargetEntityType;
        }
    }
}