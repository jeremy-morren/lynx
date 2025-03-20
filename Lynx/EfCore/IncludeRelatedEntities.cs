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
    /// Gets all properties that should be included for the entity type, excluding cyclic references to <paramref name="parentProperty"/>
    /// </summary>
    /// <param name="model">Model</param>
    /// <param name="entityType">Entity type to get include properties for</param>
    /// <param name="parentEntityType">Type of parent entity to exclude cyclic references to</param>
    /// <param name="parentProperty">Property on <paramref name="parentEntityType"/> to exclude cyclic references to</param>
    /// <remarks>
    /// parentEntityType may be different to parentProperty.DeclaringType if the property is defined on a base class.
    /// </remarks>
    public static IEnumerable<string> GetIncludeProperties(IModel model, Type entityType, Type parentEntityType, PropertyInfo parentProperty)
    {
        Debug.Assert(parentProperty.DeclaringType != null && parentProperty.DeclaringType.IsAssignableFrom(parentEntityType),
            "parentEntityType must be assignable from parentProperty.DeclaringType");

        var parentEntity = model.GetEntityType(parentEntityType);
        var navigation = parentEntity.GetNavigations().FirstOrDefault(n => n.PropertyInfo == parentProperty);
        return GetIncludePropertiesInternal(model.GetEntityType(entityType), navigation?.ForeignKey);
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
}