using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Providers.Common.Models;

/// <summary>
/// Owned entity information.
/// </summary>
internal class OwnedEntityInfo : EntityInfo, IStructurePropertyInfo
{
    /// <summary>
    /// Full path to the owned entity property
    /// </summary>
    public required PropertyChain Name { get; init; }

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
    public required PropertyChain ColumnName { get; init; }

    IPropertyBase IEntityPropertyInfo.Property => Navigation;

    /// <summary>
    /// Gets a value indicating whether this property is a collection of owned entities.
    /// </summary>
    public bool IsCollection => Navigation.IsCollection;
}