using System.Reflection;

namespace Lynx.Provider.Common;

/// <summary>
/// Information about a CLR property of an entity.
/// </summary>
internal class EntityClrPropertyInfo
{
    /// <summary>
    /// Owning entity type
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// Property info
    /// </summary>
    public required PropertyInfo PropertyInfo { get; init; }

    /// <summary>
    /// Column name
    /// </summary>
    public required ColumnName ColumnName { get; init; }
}