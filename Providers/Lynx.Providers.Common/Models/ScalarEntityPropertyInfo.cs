using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.Providers.Common.Models;

/// <summary>
/// Scalar property information.
/// </summary>
internal class ScalarEntityPropertyInfo : EntityPropertyInfo<IProperty>, IColumnPropertyInfo
{
    /// <summary>
    /// Ordinal position of the column in the table (including key columns).
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    /// <summary>
    /// Type mapping for Scalar properties
    /// </summary>
    public RelationalTypeMapping TypeMapping => Property.GetRelationalTypeMapping();
}