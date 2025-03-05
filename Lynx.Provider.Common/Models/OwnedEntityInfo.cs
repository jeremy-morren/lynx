using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common.Models;

/// <summary>
/// Owned entity information.
/// </summary>
internal class OwnedEntityInfo : EntityInfo, IEntityPropertyInfo
{
    /// <summary>
    /// Owning entity type
    /// </summary>
    public required ITypeBase Parent { get; init; }

    /// <summary>
    /// Navigation to the owned entity
    /// </summary>
    public required INavigation Navigation { get; init; }

    /// <summary>
    /// Owned entity type
    /// </summary>
    public required IEntityType EntityType { get; init; }

    /// <inheritdoc />
    public required PropertyInfo PropertyInfo { get; init; }

    /// <inheritdoc />
    public required ColumnName ColumnName { get; init; }

    public bool IsJson => EntityType.IsMappedToJson();

    IPropertyBase IEntityPropertyInfo.Property => Navigation;
}