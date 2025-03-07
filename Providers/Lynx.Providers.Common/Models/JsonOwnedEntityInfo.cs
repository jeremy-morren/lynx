namespace Lynx.Providers.Common.Models;

/// <summary>
/// Owned entity information when the owned entity is stored in a JSON column.
/// </summary>
internal class JsonOwnedEntityInfo : OwnedEntityInfo, IColumnPropertyInfo
{
    /// <summary>
    /// JSON column index
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    public static JsonOwnedEntityInfo New(OwnedEntityInfo source) => new()
    {
        Name = source.Name,
        Parent = source.Parent,
        Type = source.Type,
        Navigation = source.Navigation,
        EntityType = source.EntityType,
        ScalarProps = source.ScalarProps,
        ComplexProps = source.ComplexProps,
        PropertyInfo = source.PropertyInfo,
        Owned = source.Owned,
        ColumnName = source.ColumnName
    };
}