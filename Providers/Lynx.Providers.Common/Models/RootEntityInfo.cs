namespace Lynx.Providers.Common.Models;

/// <summary>
/// Root entity (i.e. not owned by any other entity).
/// </summary>
internal abstract class RootEntityInfo : EntityInfo
{
    /// <summary>
    /// Key properties
    /// </summary>
    public required IReadOnlyList<ScalarEntityPropertyInfo> Keys { get; init; }

    /// <summary>
    /// Database table name
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Database schema name (if any)
    /// </summary>
    public required string? Schema { get; init; }
}

/// <summary>
/// Root entity (i.e. not owned by any other entity).
/// </summary>
internal class RootEntityInfo<T> : RootEntityInfo
    where T : class
{

}