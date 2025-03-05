using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore.Helpers;

internal static class EfCoreHelpers
{
    /// <summary>
    /// Gets the entity type from the model or throws.
    /// </summary>
    public static IEntityType GetEntityType(this IModel model, Type entityType) => 
        model.FindEntityType(entityType) ?? throw new InvalidOperationException($"Type {entityType} not registered in model");

    /// <summary>
    /// Gets the primary key of an entity type or throws.
    /// </summary>
    public static IKey GetPrimaryKey(this IEntityType entityType) =>
        entityType.FindPrimaryKey() ?? throw new InvalidOperationException($"Entity {entityType} does not have a primary key.");
}