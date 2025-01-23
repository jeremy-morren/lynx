using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.ExpressionTranslators.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace NodaTime.Absolute.EFCore.Query.ExpressionTranslators;

internal class AbsoluteDateTimeMethodTranslatorPlugin(
    ISqlExpressionFactory sqlExpressionFactory,
    IEnumerable<IRelationalTypeMappingSourcePlugin> typeMappingSourcePlugins)
    : IMethodCallTranslatorPlugin
{
    public IEnumerable<IMethodCallTranslator> Translators { get; } =
    [
        new AbsoluteDateTimeMethodCallTranslator(sqlExpressionFactory, typeMappingSourcePlugins.ToList())
    ];
}