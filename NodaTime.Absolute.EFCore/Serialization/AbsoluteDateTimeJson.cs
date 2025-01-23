using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NodaTime.Absolute.EFCore.Serialization;

/// <summary>
/// EF JSON representation of <see cref="AbsoluteDateTime"/>.
/// </summary>
internal readonly record struct AbsoluteDateTimeJson
{
    /// <summary>
    /// Timezone
    /// </summary>
    public required string ZoneId { get; init; }

    /// <summary>
    /// Absolute value of the date and time
    /// </summary>
    [JsonConverter(typeof(InstantConverter))]
    public required Instant Instant { get; init; }

    /// <summary>
    /// Local date value
    /// </summary>
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public required LocalDateTime Local { get; init; }

    public static AbsoluteDateTimeJson FromDate(ZonedDateTime value) =>
        new()
        {
            ZoneId = value.Zone.Id,
            Instant = value.ToInstant(),
            Local = value.LocalDateTime
        };

    public static ZonedDateTime FromJsonElement(JsonElement json, IDateTimeZoneProvider provider)
    {
        var value = json.Deserialize(JsonTypeInfo);
        return value.Instant.InZone(provider[value.ZoneId]);
    }

    public static ZonedDateTime FromJson(string json, IDateTimeZoneProvider provider)
    {
        var value = JsonSerializer.Deserialize(json, JsonTypeInfo);
        return value.Instant.InZone(provider[value.ZoneId]);
    }

    public JsonElement ToJsonElement() => JsonSerializer.SerializeToElement(this, JsonTypeInfo);
    public string ToJson() => JsonSerializer.Serialize(this, JsonTypeInfo);

    private static readonly JsonTypeInfo<AbsoluteDateTimeJson> JsonTypeInfo =
        AbsoluteDateTimeJsonSerializerContext.Default.AbsoluteDateTimeJson;
}