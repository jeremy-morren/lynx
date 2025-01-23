using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime.Absolute.EFCore.Serialization;

namespace NodaTime.Absolute.EFCore.Query.ExpressionTranslators;

public class AbsoluteDateTimeMethodCallTranslator : IMethodCallTranslator
{
    private readonly ISqlExpressionFactory _factory;
    private readonly RelationalTypeMapping _instantTypeMapping;

    public AbsoluteDateTimeMethodCallTranslator(
        ISqlExpressionFactory factory,
        List<IRelationalTypeMappingSourcePlugin> mappingPlugins)
    {
        _factory = factory;
        _instantTypeMapping = GetTypeMapping(typeof(Instant), mappingPlugins)!;
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
                _instantTypeMapping,
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

    private RelationalTypeMapping? GetTypeMapping(Type type, List<IRelationalTypeMappingSourcePlugin> mappingPlugins)
    {
        var info = new RelationalTypeMappingInfo(type: type);
        foreach (var t in mappingPlugins)
            if (t.FindMapping(info) is { } mapping)
                return mapping;
        throw new InvalidOperationException($"Type mapping for {type} not found. Ensure that the provider-specific plugin for NodaTime is registered.");
    }

    private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.Instance;

    private static readonly MethodInfo ToInstantMethod =
        typeof(AbsoluteDateTime).GetMethod(nameof(AbsoluteDateTime.ToInstant), InstanceMembers)!;

    private static readonly MethodInfo GetZoneIdMethod =
        typeof(AbsoluteDateTime).GetMethod(nameof(AbsoluteDateTime.GetZoneId), InstanceMembers)!;

}