using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace NodaTime.Absolute.EFCore.Query.ExpressionTranslators;

internal class AbsoluteDateTimeTimeTranslatorBase
{
    private readonly List<IRelationalTypeMappingSourcePlugin> _mappingPlugins;

    protected readonly RelationalTypeMapping InstantTypeMapping;
    protected readonly RelationalTypeMapping LocalDateTimeMapping;

    protected AbsoluteDateTimeTimeTranslatorBase(IEnumerable<IRelationalTypeMappingSourcePlugin> mappingPlugins)
    {
        _mappingPlugins = mappingPlugins.ToList();

        InstantTypeMapping = GetTypeMapping(typeof(Instant));
        LocalDateTimeMapping = GetTypeMapping(typeof(LocalDateTime));
    }

    private RelationalTypeMapping GetTypeMapping(Type type)
    {
        var info = new RelationalTypeMappingInfo(type: type);
        foreach (var t in _mappingPlugins)
            if (t.FindMapping(info) is { } mapping)
                return mapping;
        throw new InvalidOperationException($"Type mapping for {type} not found. Ensure that the provider-specific plugin for NodaTime is registered.");
    }

    public const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.Instance;

}