using Lynx.Provider.Common.Models;

namespace Lynx.Provider.Common;

internal static class EntityModelExtensions
{
    /// <summary>
    /// Gets all scalar properties of the entity, including nested properties.  Excludes key properties.
    /// </summary>
    public static IEnumerable<ScalarEntityPropertyInfo> GetAllScalarProps(this IStructureEntity entity)
    {
        return entity.ScalarProps.Concat(entity.ComplexProps.SelectMany(c => c.GetAllScalarProps()));
    }
}