using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common;

internal static class EntityInfoFactory<T> where T : class
{
    public static EntityInfo Create(IModel model)
    {
        var entityType = model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} not found in model.");

        var owned = entityType

    }

    private static EntityInfo CreateInternal(IModel model, ColumnName? parent)
    {

    }
}