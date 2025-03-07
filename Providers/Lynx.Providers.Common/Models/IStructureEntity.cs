namespace Lynx.Providers.Common.Models;

/// <summary>
/// Base class for an entity with properties.
/// </summary>
internal interface IStructureEntity
{
    /// <summary>
    /// Scalar properties
    /// </summary>
    IReadOnlyList<ScalarEntityPropertyInfo> ScalarProps { get; init; }

    /// <summary>
    /// Complex properties
    /// </summary>
    IReadOnlyList<ComplexEntityPropertyInfo> ComplexProps { get; init; }

}