using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.ForeignKeys;

internal static class IncludeRelatedEntities
{
    /// <summary>
    /// Gets all properties that should be included for the entity type.
    /// </summary>
    public static IEnumerable<string> GetIncludeProperties(IModel model, Type entityType) =>
        GetIncludePropertiesInternal(GetEntityType(model, entityType), null);
    
    private static IEnumerable<string> GetIncludePropertiesInternal(IEntityType entityType, IForeignKey? parentKey) =>
        GetNavigations(entityType, parentKey)
            .SelectMany(nav =>
            {
                //Recursively get all children, except for this navigation
                var children = GetIncludePropertiesInternal(nav.TargetEntityType, nav.ForeignKey)
                    .Select(n => $"{nav.Name}.{n}");

                return children.Prepend(nav.Name); // Return the navigation itself and its children
            });

    /// <summary>
    /// Gets all navigations for the entity type that should be included.
    /// Optionally exclude the navigation with the parent key.
    /// </summary>
    public static IEnumerable<INavigation> GetNavigations(IModel model, Type entityType, IForeignKey? parentKey)
    {
        return GetNavigations(GetEntityType(model, entityType), parentKey);
    }

    private static IEnumerable<INavigation> GetNavigations(IEntityType entity, IForeignKey? parentKey)
    {
        // Get all navigations
        // Exclude collections and owned properties
        // Exclude parent key (if properties on both sides are defined) to stop infinite recursion
        return entity.GetNavigations()
            .Where(nav => nav.ForeignKey != parentKey 
                          && nav is { IsCollection: false, ForeignKey.IsOwnership: false });
    }
    
    public static DbContext GetDbContext(IQueryable query)
    {
        if (query is not IInfrastructure<IServiceProvider> src)
            throw new InvalidOperationException("Query is not an EF Core query");
        return src.Instance.GetRequiredService<ICurrentDbContext>().Context;
    }
    
    public static IEntityType GetEntityType(IModel model, Type entityType) => 
        model.FindEntityType(entityType) ?? throw new InvalidOperationException($"Type {entityType} not registered in model");
}