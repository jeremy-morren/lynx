using Lynx.EfCore;

namespace Lynx;

/// <summary>
/// Configure Lynx to ignore the referenced property in <see cref="IncludeRelatedEntitiesQueryableExtensions.IncludeAllReferenced{T}"/>.
/// When this attribute is specified, then the navigation property must be included manually.
/// </summary>
/// <remarks>
/// <para>
/// This attribute does not affect <see cref="ReferencingEntitiesIModelExtensions.GetReferencingEntities"/>
/// </para>
/// <para>
/// This attribute can be used to stop Lynx from including a large object graph when querying entities.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LynxDoNotIncludeReferencedAttribute : Attribute;