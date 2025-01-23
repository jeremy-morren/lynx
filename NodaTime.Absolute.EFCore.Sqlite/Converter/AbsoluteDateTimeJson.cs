using System.Text.Json.Serialization;

namespace NodaTime.Absolute.EFCore.Sqlite.Converter;

/// <summary>
/// EF JSON representation of <see cref="AbsoluteDateTime"/>.
/// </summary>
internal readonly record struct ZonedDateTimeJson
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

    public static implicit operator ZonedDateTimeJson(AbsoluteDateTime value) =>
        new()
        {
            ZoneId = value.Zone.Id,
            Instant = value.ToInstant(),
            Local = value.LocalDateTime
        };

    public ZonedDateTime ToDateTime(IDateTimeZoneProvider provider) => Instant.InZone(provider[ZoneId]);
}