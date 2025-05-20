using NodaTime;

namespace Lynx.Providers.Tests;

public static class MillisecondClock
{
    /// <summary>
    /// Now, accurate to milliseconds
    /// </summary>
    public static Instant Now => Instant.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    public static LocalDateTime LocalNow => Now.InZone(DateTimeZoneProviders.Bcl.GetSystemDefault()).LocalDateTime;
}