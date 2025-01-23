using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime.Absolute.EFCore.Serialization;

namespace NodaTime.Absolute.EFCore.Query.ExpressionTranslators;

internal class AbsoluteDateTimeMethodCallTranslator : AbsoluteDateTimeTimeTranslatorBase, IMethodCallTranslator
{
    public AbsoluteDateTimeMethodCallTranslator(
        IEnumerable<IRelationalTypeMappingSourcePlugin> mappingPlugins)
        : base(mappingPlugins)
    {

    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance == null) return null;

        // Translate calls to AbsoluteDateTime.ToInstant() to Instant property
        if (method == ToInstantMethod)
            return new JsonScalarExpression(instance,
                [new PathSegment(nameof(AbsoluteDateTimeJson.Instant))],
                typeof(Instant),
                InstantTypeMapping,
                false);

        // Translate calls to AbsoluteDateTime.GetZoneId() to ZoneId property
        if (method == GetZoneIdMethod)
            return new JsonScalarExpression(instance,
                [new PathSegment(nameof(AbsoluteDateTimeJson.ZoneId))],
                typeof(string),
                StringTypeMapping.Default,
                false);

        return null;
    }

    private static readonly MethodInfo ToInstantMethod =
        typeof(AbsoluteDateTime).GetMethod(nameof(AbsoluteDateTime.ToInstant), InstanceMembers)!;

    private static readonly MethodInfo GetZoneIdMethod =
        typeof(AbsoluteDateTime).GetMethod(nameof(AbsoluteDateTime.GetZoneId), InstanceMembers)!;

}