using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common.Models;

/// <summary>
/// Information about a CLR property of an entity.
/// </summary>
[DebuggerDisplay($"{{{nameof(Property)}}}")]
internal abstract class EntityPropertyInfo<T> : IEntityPropertyInfo where T : IPropertyBase
{
    /// <summary>
    /// Underlying EF Core property
    /// </summary>
    public required T Property { get; init; }

    /// <inheritdoc />
    public required ITypeBase Parent { get; init; }

    /// <inheritdoc />
    public required PropertyInfo PropertyInfo { get; init; }

    /// <inheritdoc />
    public required ColumnName ColumnName { get; init; }
    
    IPropertyBase IEntityPropertyInfo.Property => Property;
}