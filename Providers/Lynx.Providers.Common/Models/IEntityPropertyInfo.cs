using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Providers.Common.Models;

internal interface IEntityPropertyInfo
{
    /// <summary>
    /// Property name (full path)
    /// </summary>
    PropertyChain Name { get; }

    /// <summary>
    /// Owning entity type
    /// </summary>
    ITypeBase Parent { get; }
    
    /// <summary>
    /// Property info
    /// </summary>
    IPropertyBase Property { get; }

    /// <summary>
    /// Property info
    /// </summary>
    PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// SQL Column name (<see cref="Name"/> adjusted for SQL)
    /// </summary>
    PropertyChain ColumnName { get; }
}