using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common.Models;

internal interface IEntityPropertyInfo
{
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
    /// Full column name
    /// </summary>
    ColumnName ColumnName { get; }
}