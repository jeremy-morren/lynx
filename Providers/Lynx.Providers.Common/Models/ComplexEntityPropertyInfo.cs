using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Providers.Common.Models;

/// <summary>
/// Complex entity property information.
/// </summary>
internal class ComplexEntityPropertyInfo : EntityPropertyInfo<IComplexProperty>, IStructurePropertyInfo
{
    /// <inheritdoc />
    public required IReadOnlyList<ScalarEntityPropertyInfo> ScalarProps { get; init; }

    /// <inheritdoc />
    public required IReadOnlyList<ComplexEntityPropertyInfo> ComplexProps { get; init; }
}