using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime.Text;

namespace NodaTime.Absolute.EFCore.Serialization;

internal class InstantConverter : JsonConverter<Instant>
{
    private static readonly InstantPattern Pattern =
        InstantPattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd HH':'mm':'ss'.'fffffff'Z'");

    public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Pattern.Parse(reader.GetString()!).GetValueOrThrow();
    }

    public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Pattern.Format(value));
    }
}

internal class LocalDateTimeConverter : JsonConverter<LocalDateTime>
{
    private static readonly LocalDateTimePattern Pattern =
        LocalDateTimePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd HH':'mm':'ss'.'fffffff");

    public override LocalDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Pattern.Parse(reader.GetString()!).GetValueOrThrow();
    }

    public override void Write(Utf8JsonWriter writer, LocalDateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Pattern.Format(value));
    }
}