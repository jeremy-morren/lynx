namespace Lynx.Providers.Common.Models;

/// <summary>
/// Complex property information when the complex type is stored in a JSON column
/// (i.e. <c>ComplexProperty(...).ToJson()</c> or <c>ComplexCollection(...).ToJson()</c>).
/// </summary>
internal class JsonComplexEntityPropertyInfo : ComplexEntityPropertyInfo, IColumnPropertyInfo
{
    /// <summary>
    /// JSON column index
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    /// <summary>
    /// Gets a value indicating whether this property is a collection of complex objects.
    /// </summary>
    public bool IsCollection => Property.IsCollection;
}
