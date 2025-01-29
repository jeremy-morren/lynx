using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime.Absolute.EFCore.Serialization;

namespace NodaTime.Absolute.EFCore.Storage.Converters;

internal class AbsoluteDateTimeJsonValueConverter : ValueConverter<AbsoluteDateTime, JsonElement>
{
    public AbsoluteDateTimeJsonValueConverter(IDateTimeZoneProvider timeZoneProvider)
        : base(
            dt => AbsoluteDateTimeJson.FromDate(dt).ToJsonElement(),
            json => AbsoluteDateTimeJson.FromJsonElement(json, timeZoneProvider),
            false)
    {
    }
}