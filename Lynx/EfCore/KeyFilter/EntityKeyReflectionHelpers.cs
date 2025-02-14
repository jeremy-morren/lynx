using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Lynx.EfCore.KeyFilter;

internal static class EntityKeyReflectionHelpers
{
    public const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    public const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    /// <summary>
    /// <see cref="Property"/> method.
    /// </summary>
    public static readonly MethodInfo EfPropertyMethod =
        typeof(EF).GetMethod(nameof(EF.Property), StaticFlags)!;

    /// <summary>
    /// <see cref="Enumerable.Contains{TSource}(System.Collections.Generic.IEnumerable{TSource},TSource)"/>
    /// </summary>
    public static readonly MethodInfo EnumerableContainsMethod =
        typeof(Enumerable).GetMethods(StaticFlags)
            .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

    /// <summary>
    /// IReadOnlyDictionary{string, object} type.
    /// </summary>
    public static readonly Type DictionaryType = typeof(IReadOnlyDictionary<string, object>);

    /// <summary>
    /// Gets IReadOnlyDictionary{string, object}.get_Item method.
    /// </summary>
    public static MethodInfo GetGetItemMethod(Type type) =>
        type.GetMethod("get_Item", InstanceFlags)
        ?? throw new InvalidOperationException($"Type {type} does not have get_Item method.");

    /// <summary>
    /// See <see cref="ExpressionCaptureValue.CaptureValue{T}"/> method.
    /// </summary>
    private static readonly MethodInfo CaptureValueMethod =
        typeof(ExpressionCaptureValue).GetMethod(nameof(ExpressionCaptureValue.CaptureValue), StaticFlags)!;

    /// <summary>
    /// Gets <see cref="ExpressionCaptureValue.CaptureValue{T}"/> method
    /// </summary>
    public static MethodInfo GetCaptureValueMethod(Type type) => CaptureValueMethod.MakeGenericMethod(type);
}