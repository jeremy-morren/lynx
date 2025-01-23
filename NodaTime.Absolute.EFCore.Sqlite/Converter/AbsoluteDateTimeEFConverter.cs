using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NodaTime.Absolute.EFCore.Sqlite.Converter;

internal class AbsoluteDateTimeEFConverter : ValueConverter<AbsoluteDateTime, string>
{
    public AbsoluteDateTimeEFConverter(
        IDateTimeZoneProvider timeZoneProvider,
        ConverterMappingHints? mappingHints = null)
        : base(
            value => Serialize(value),
            value => Deserialize(value, timeZoneProvider),
            false,
            mappingHints)
    {
    }

    public static string Serialize(AbsoluteDateTime value) =>
        JsonSerializer.Serialize(value, JsonTypeInfo);

    public static AbsoluteDateTime Deserialize(string value, IDateTimeZoneProvider timeZoneProvider) =>
        JsonSerializer.Deserialize(value, JsonTypeInfo).ToDateTime(timeZoneProvider);

    private static readonly JsonTypeInfo<ZonedDateTimeJson> JsonTypeInfo =
        AbsoluteDateTimeJsonSerializerContext.Default.ZonedDateTimeJson;
}
