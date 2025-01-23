using System.Text.Json;
using System.Text.Json.Serialization;

namespace NodaTime.Absolute.EFCore.Sqlite.Converter;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.General,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(ZonedDateTimeJson))]
internal partial class AbsoluteDateTimeJsonSerializerContext : JsonSerializerContext;