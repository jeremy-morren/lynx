using System.Text.Json;
using System.Text.Json.Serialization;

namespace NodaTime.Absolute.EFCore.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.General,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(AbsoluteDateTimeJson))]
internal partial class AbsoluteDateTimeJsonSerializerContext : JsonSerializerContext;