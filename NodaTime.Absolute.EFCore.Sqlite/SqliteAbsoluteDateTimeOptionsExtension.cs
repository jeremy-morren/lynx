using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace NodaTime.Absolute.EFCore.Sqlite;

public class AbsoluteDateTimeOptionsExtension : IDbContextOptionsExtension
{
    private readonly IDateTimeZoneProvider _timeZoneProvider;

    public AbsoluteDateTimeOptionsExtension(IDateTimeZoneProvider? timeZoneProvider = null)
    {
        _timeZoneProvider = timeZoneProvider ?? DateTimeZoneProviders.Tzdb;
    }

    public void ApplyServices(IServiceCollection services)
    {
        throw new NotImplementedException();
    }

    public void Validate(IDbContextOptions options)
    {
        throw new NotImplementedException();
    }

    public DbContextOptionsExtensionInfo Info => throw new NotImplementedException();
}