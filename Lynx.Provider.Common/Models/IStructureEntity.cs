namespace Lynx.Provider.Common.Models;

/// <summary>
/// Base class for an entity with properties.
/// </summary>
internal interface IStructureEntity
{
    /// <summary>
    /// Scalar properties
    /// </summary>
    IReadOnlyList<EntityPropertyInfo> ScalarProps { get; init; }

    /// <summary>
    /// Complex properties
    /// </summary>
    IReadOnlyList<ComplexEntityPropertyInfo> ComplexProps { get; init; }

}