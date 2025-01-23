using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NodaTime;

/// <summary>
/// JSON converter for <see cref="AbsoluteDateTime"/>.
/// </summary>
internal class AbsoluteDateTimeJsonConverter : JsonConverter<AbsoluteDateTime>
{
    public override AbsoluteDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<ZonedDateTime>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, AbsoluteDateTime value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (ZonedDateTime)value, options);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, AbsoluteDateTime value, JsonSerializerOptions options)
    {
        var element = JsonSerializer.SerializeToElement((ZonedDateTime)value, options);
        writer.WritePropertyName(element.GetString()!);
    }

    public override AbsoluteDateTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString() ?? throw new JsonException("Value was null");

        return JsonValue.Create(str).Deserialize<ZonedDateTime>(options);
    }
}