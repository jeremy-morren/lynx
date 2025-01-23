using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using NodaTime.Absolute.EFCore.Serialization;
using NodaTime.Absolute.EFCore.Storage.Converters;

namespace NodaTime.Absolute.EFCore.Storage;

internal class AbsoluteDateTimeTypeMapping : RelationalTypeMapping
{
    public AbsoluteDateTimeTypeMapping(IDateTimeZoneProvider timeZoneProvider)
        : this(CreateParameters(timeZoneProvider))
    {
    }

    protected AbsoluteDateTimeTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override string SqlLiteralFormatString => "'{0}'";

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) =>
        new AbsoluteDateTimeTypeMapping(parameters);

    private static RelationalTypeMappingParameters CreateParameters(IDateTimeZoneProvider timeZoneProvider) =>
        new(new CoreTypeMappingParameters(
                typeof(AbsoluteDateTime),
                new AbsoluteDateTimeValueConverter(timeZoneProvider),
                jsonValueReaderWriter: new AbsoluteDateTimeValueJsonReaderWriter(timeZoneProvider)),
            "TEXT");
}