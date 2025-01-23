using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime.Absolute.EFCore.Serialization;

namespace NodaTime.Absolute.EFCore.Storage.Converters;

internal class AbsoluteDateTimeValueConverter : ValueConverter<AbsoluteDateTime, string>
{
    public AbsoluteDateTimeValueConverter(IDateTimeZoneProvider timeZoneProvider)
        : base(
            dt => AbsoluteDateTimeJson.FromDate(dt).ToJson(),
            json => AbsoluteDateTimeJson.FromJson(json, timeZoneProvider),
            false)
    {
    }
}