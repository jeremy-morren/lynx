namespace Lynx.Providers.Common.Models;

/// <summary>
/// Represents a property that is mapped to a database column.
/// </summary>
internal interface IColumnPropertyInfo : IEntityPropertyInfo
{
    int ColumnIndex { get; }
}