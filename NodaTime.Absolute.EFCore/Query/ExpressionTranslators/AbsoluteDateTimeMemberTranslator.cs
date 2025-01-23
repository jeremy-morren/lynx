using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime.Absolute.EFCore.Serialization;

namespace NodaTime.Absolute.EFCore.Query.ExpressionTranslators;

internal class AbsoluteDateTimeMemberTranslator : AbsoluteDateTimeTimeTranslatorBase, IMemberTranslator
{
    public AbsoluteDateTimeMemberTranslator(
        IEnumerable<IRelationalTypeMappingSourcePlugin> mappingPlugins)
        : base(mappingPlugins)
    {
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance == null)
            return null;

        if (returnType == typeof(AbsoluteDateTime))
        {
        }

        if (member.DeclaringType != typeof(AbsoluteDateTime))
            return null; // Not a member of AbsoluteDateTime

        return member.Name switch
        {
            // Translate to Local json property
            nameof(AbsoluteDateTime.LocalDateTime) =>
                new JsonScalarExpression(instance,
                    [new PathSegment(nameof(AbsoluteDateTimeJson.Local))],
                    typeof(LocalDateTime),
                    LocalDateTimeMapping,
                    false),
            _ => null
        };
    }

    private static readonly PropertyInfo LocalDateTimeProperty =
        typeof(AbsoluteDateTime).GetProperty(nameof(AbsoluteDateTime.LocalDateTime), InstanceMembers)!;
}