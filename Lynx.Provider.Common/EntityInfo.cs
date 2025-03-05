namespace Lynx.Provider.Common;

/// <summary>
/// EF core entity information.
/// </summary>
internal class EntityInfo
{
    /// <summary>
    /// Entity type
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// Properties (other than keys)
    /// </summary>
    public required IReadOnlyList<EntityClrPropertyInfo> Properties { get; init; }

    /// <summary>
    /// Owned entities
    /// </summary>
    public required IReadOnlyList<EntityInfo> Owned { get; init; }
}