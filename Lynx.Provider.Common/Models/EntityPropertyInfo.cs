using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common.Models;

/// <summary>
/// Information about a CLR property of an entity.
/// </summary>
[DebuggerDisplay($"{{{nameof(Property)}}}")]
internal class EntityPropertyInfo : IEntityPropertyInfo
{
    /// <summary>
    /// Underlying EF Core property
    /// </summary>
    public required IPropertyBase Property { get; init; }

    /// <inheritdoc />
    public required ITypeBase Parent { get; init; }

    /// <inheritdoc />
    public required PropertyInfo PropertyInfo { get; init; }

    /// <inheritdoc />
    public required ColumnName ColumnName { get; init; }
}

internal interface IEntityPropertyInfo
{
    /// <summary>
    /// Owning entity type
    /// </summary>
    ITypeBase Parent { get; }

    /// <summary>
    /// Property info
    /// </summary>
    PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// Full column name
    /// </summary>
    ColumnName ColumnName { get; }
}