using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Providers.Common.Models;

/// <summary>
/// EF core entity information.
/// </summary>
[DebuggerDisplay($"{{{nameof(Type)}}}")]
internal class EntityInfo : IStructureEntity
{
    /// <summary>
    /// Entity type
    /// </summary>
    public required IEntityType Type { get; init; }

    /// <inheritdoc />
    public required IReadOnlyList<ScalarEntityPropertyInfo> ScalarProps { get; init; }

    /// <inheritdoc />
    public required IReadOnlyList<ComplexEntityPropertyInfo> ComplexProps { get; init; }

    /// <summary>
    /// Owned entities
    /// </summary>
    public required IReadOnlyList<OwnedEntityInfo> Owned { get; init; }
}