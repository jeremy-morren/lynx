using Lynx.Providers.Common.Models;

namespace Lynx.Providers.Common.Entities;

internal static class EntityModelExtensions
{
    /// <summary>
    /// Gets all scalar columns of the entity, including nested properties.  Excludes key properties.
    /// </summary>
    public static IEnumerable<IColumnPropertyInfo> GetAllScalarColumns(this IStructureEntity entity)
    {
        var owned = (entity as EntityInfo)?.Owned ?? [];

        return entity.ScalarProps
            .Concat(entity.ComplexProps.SelectMany(c => c.GetAllScalarColumns()))
            .Concat(owned.SelectMany(c=> c.GetOwnedScalarColumns()));
    }

    private static IEnumerable<IColumnPropertyInfo> GetOwnedScalarColumns(this OwnedEntityInfo owned)
    {
        if (owned is JsonOwnedEntityInfo json)
            return [json]; // JSON columns are scalar

        // Not a JSON column, recurse
        return GetAllScalarColumns(owned);
    }
}