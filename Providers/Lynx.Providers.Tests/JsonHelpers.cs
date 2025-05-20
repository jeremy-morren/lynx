using System.Text.Json;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Lynx.Providers.Tests;

public static class JsonHelpers
{
    public static string? SerializeJson(object? value)
    {
        var jsonOptions = new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        return value == null ? null : JsonSerializer.Serialize(value, jsonOptions);
    }
}