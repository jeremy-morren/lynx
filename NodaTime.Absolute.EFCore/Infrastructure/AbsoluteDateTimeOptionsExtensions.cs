using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Absolute.EFCore.Storage;

namespace NodaTime.Absolute.EFCore.Infrastructure;

internal class AbsoluteDateTimeOptionsExtension : IDbContextOptionsExtension
{
    private readonly IDateTimeZoneProvider _timeZoneProvider;

    public AbsoluteDateTimeOptionsExtension(IDateTimeZoneProvider? timeZoneProvider)
    {
        _timeZoneProvider = timeZoneProvider ?? DateTimeZoneProviders.Tzdb;
        Info = new ExtensionInfo(this, _timeZoneProvider);
    }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddEntityFrameworkAbsoluteDateTime(_timeZoneProvider);
    }

    public void Validate(IDbContextOptions options)
    {
        var provider = options.FindExtension<CoreOptionsExtension>()?.InternalServiceProvider;
        if (provider is null)
        {
            return;
        }

        using var scope = provider.CreateScope();

        var services = scope.ServiceProvider
            .GetServices<IRelationalTypeMappingSourcePlugin>()
            .OfType<AbsoluteDateTimeTypeMappingSourcePlugin>();

        if (services.Any())
        {
            throw new InvalidOperationException($"{nameof(AbsoluteDateTimeDbContextOptionsBuilderExtensions.UseAbsoluteDateTime)} requires {nameof(AbsoluteDateTimeServiceCollectionExtensions.AddEntityFrameworkAbsoluteDateTime)} to be called on the internal service provider used.");
        }
    }

    public DbContextOptionsExtensionInfo Info { get; }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private readonly IDateTimeZoneProvider _timeZoneProvider;

        public ExtensionInfo(IDbContextOptionsExtension extension, IDateTimeZoneProvider timeZoneProvider)
            : base(extension)
        {
            _timeZoneProvider = timeZoneProvider;
        }

        public override bool IsDatabaseProvider => false;

        public override int GetServiceProviderHashCode() => _timeZoneProvider.GetHashCode();

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo[$"NodaTime:{nameof(AbsoluteDateTime)}"] = "1";

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo otherInfo && otherInfo._timeZoneProvider.Equals(_timeZoneProvider);

        public override string LogFragment => $"using {typeof(AbsoluteDateTime).FullName} ";
    }
}