using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace NodaTime.Absolute.EFCore.Query.ExpressionTranslators;

internal class AbsoluteDateTimeMemberTranslatorPlugin(
    IEnumerable<IRelationalTypeMappingSourcePlugin> mappingPlugins)
    : IMemberTranslatorPlugin
{
    public IEnumerable<IMemberTranslator> Translators { get; } =
    [
        new AbsoluteDateTimeMemberTranslator(mappingPlugins),
    ];
}