using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime.Text;

namespace NodaTime.Absolute.EFCore.Sqlite.Converter;

internal class InstantConverter : JsonConverter<Instant>
{
    /// <summary>
    /// Sqlite only supports 3 decimal places for fractional seconds.
    /// </summary>
    private static readonly InstantPattern Pattern =
        InstantPattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

    public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Pattern.Parse(reader.GetString()!).Value;
    }

    public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Pattern.Format(value));
    }
}

internal class LocalDateTimeConverter : JsonConverter<LocalDateTime>
{
    /// <summary>
    /// Sqlite only supports 3 decimal places for fractional seconds.
    /// </summary>
    private static readonly LocalDateTimePattern Pattern =
        LocalDateTimePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd'T'HH':'mm':'ss'.'fff");

    public override LocalDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Pattern.Parse(reader.GetString()!).Value;
    }

    public override void Write(Utf8JsonWriter writer, LocalDateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Pattern.Format(value));
    }
}