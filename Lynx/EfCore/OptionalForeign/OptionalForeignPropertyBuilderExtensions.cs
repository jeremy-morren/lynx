using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Lynx.EfCore.OptionalForeign;

public static class OptionalForeignPropertyBuilderExtensions
{
    /// <summary>
    /// <para>Lynx: Configures an entity as an optional foreign entity (i.e. may exist or not).</para>
    /// <para>NB: The related entity must be registered in the model separately.</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="key"></param>
    /// <param name="navigation"></param>
    /// <typeparam name="TPrincipal"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TRelatedEntity"></typeparam>
    /// <remarks>
    /// This does not add a foreign key constraint to the database.
    /// Use <see cref="OptionalForeignPropertyIncludeExtensions.IncludeOptionalForeign{TPrincipal,TRelatedEntity}"/>
    /// to load
    /// </remarks>
    public static void HasOptionalForeign<TPrincipal, TKey, TRelatedEntity>(
        this EntityTypeBuilder<TPrincipal> builder,
        Expression<Func<TPrincipal, TKey>> key,
        Expression<Func<TPrincipal, TRelatedEntity?>> navigation)
        where TPrincipal : class
        where TRelatedEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(navigation);

        if (key.GetMemberAccessList().Count != 1)
            throw new InvalidOperationException("Only simple keys are supported.");

        var keyProperty = key.GetMemberAccess().GetSimpleMemberName();
        var navProperty = GetNavigationPropertyName(navigation);

        builder.Ignore(navProperty); //Ignore navigation property from normal EF Core processing

        builder.HasIndex(keyProperty); //Ensure key is indexed

        builder.HasAnnotation(GetForeignKeyAnnotation(navProperty), keyProperty); //Store foreign key property name
    }

    /// <summary>
    /// Gets the name of the annotation that stores the foreign key property name.
    /// </summary>
    internal static string GetForeignKeyAnnotation(string navProperty) => $"{AnnotationPrefix}{navProperty}";

    internal const string AnnotationPrefix = "Lynx:OptionalForeignKey_";

    /// <summary>
    /// Gets the name of the navigation property.
    /// </summary>
    internal static string GetNavigationPropertyName<TPrincipal, TRelatedEntity>(Expression<Func<TPrincipal, TRelatedEntity>> expression)
    {
        if (expression.GetMemberAccessList().Count != 1)
            throw new InvalidOperationException("Unsupported navigation property.");

        return expression.GetMemberAccess().Name;
    }

    private static string GetSimpleMemberName(this MemberInfo member)
    {
        var name = member.Name;
        var index = name.LastIndexOf('.');
        return index >= 0 ? name[(index + 1)..] : name;
    }
}