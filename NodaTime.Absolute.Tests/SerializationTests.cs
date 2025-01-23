﻿using System.Text.Json;
using FluentAssertions;
using NodaTime.Absolute.EFCore.Sqlite.Converter;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Text;

namespace NodaTime.Absolute.Tests;

public class SerializationTests
{
    [Fact]
    public void JsonRoundTripShouldBeSuccessful()
    {
        AbsoluteDateTime dt = Instant.FromDateTimeUtc(DateTime.UtcNow)
            .InZone(DateTimeZoneProviders.Tzdb["America/St_Vincent"]);

        var jsonOptions = new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        var json = JsonSerializer.Serialize(dt, jsonOptions);
        JsonSerializer.Deserialize<AbsoluteDateTime>(json, jsonOptions).Should().Be(dt);
    }

    [Theory]
    [InlineData("yyyy-MM-ddTHH:mm:ss.fffZ")]
    [InlineData("yyyy-MM-ddTHH:mm:ssZ")]
    public void EFRoundTripShouldBeSuccessful(string format)
    {
        var now = InstantPattern.ExtendedIso.Parse(DateTime.UtcNow.ToString(format)).Value;

        AbsoluteDateTime dt =  now.InZone(DateTimeZoneProviders.Tzdb["Australia/Perth"]);

        var json = AbsoluteDateTimeEFConverter.Serialize(dt);
        AbsoluteDateTimeEFConverter.Deserialize(json, DateTimeZoneProviders.Tzdb).Should().Be(dt);
    }
}