using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;
using NodaTime.Absolute.EFCore.Serialization;

namespace NodaTime.Absolute.EFCore.Storage.Converters;

internal class AbsoluteDateTimeValueJsonReaderWriter : JsonValueReaderWriter<AbsoluteDateTime>
{
    private readonly IDateTimeZoneProvider _timeZoneProvider;

    public AbsoluteDateTimeValueJsonReaderWriter(IDateTimeZoneProvider timeZoneProvider)
    {
        _timeZoneProvider = timeZoneProvider;
    }

    public override AbsoluteDateTime FromJsonTyped(ref Utf8JsonReaderManager manager, object? existingObject = null)
    {
        if (manager.CurrentReader.TokenType != JsonTokenType.String)
            throw new InvalidOperationException($"Expected a string token for {typeof(AbsoluteDateTime)}");
        var json = manager.CurrentReader.GetString() ?? throw new InvalidOperationException("Value was null");
        return AbsoluteDateTimeJson.FromJson(json, _timeZoneProvider);
    }

    public override void ToJsonTyped(Utf8JsonWriter writer, AbsoluteDateTime value)
    {
        var json = AbsoluteDateTimeJson.FromDate(value).ToJson();
        writer.WriteStringValue(json);
    }
}