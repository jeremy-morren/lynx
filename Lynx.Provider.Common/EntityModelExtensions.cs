using Lynx.Provider.Common.Models;

namespace Lynx.Provider.Common;

internal static class EntityModelExtensions
{
    public static IEnumerable<EntityPropertyInfo> GetAllProperties(this IStructureEntity entity) =>
        entity.ScalarProps.Concat(entity.ComplexProps.SelectMany(c => c.GetAllProperties()));
}