using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Absolute.EFCore.Query.ExpressionTranslators;
using NodaTime.Absolute.EFCore.Storage;

namespace NodaTime.Absolute.EFCore.Infrastructure;

public static class AbsoluteDateTimeServiceCollectionExtensions
{
    /// <summary>
    /// Enables the use of <see cref="AbsoluteDateTime"/> with Entity Framework Core.
    /// </summary>
    public static void AddEntityFrameworkAbsoluteDateTime(this IServiceCollection services, IDateTimeZoneProvider timeZoneProvider)
    {
        ArgumentNullException.ThrowIfNull(timeZoneProvider);
        ArgumentNullException.ThrowIfNull(services);

        new EntityFrameworkRelationalServicesBuilder(services)
            .TryAdd<IRelationalTypeMappingSourcePlugin>(new AbsoluteDateTimeTypeMappingSourcePlugin(timeZoneProvider))
            .TryAdd<IMethodCallTranslatorPlugin, AbsoluteDateTimeMethodTranslatorPlugin>();
    }
}