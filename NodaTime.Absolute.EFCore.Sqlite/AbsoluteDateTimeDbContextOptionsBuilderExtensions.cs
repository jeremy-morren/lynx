using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace NodaTime.Absolute.EFCore.Sqlite;

public static class AbsoluteDateTimeDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables use of <see cref="AbsoluteDateTime"/> as a property type in the model.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="provider">Time zone provider (defaults to <see cref="DateTimeZoneProviders.Tzdb"/>)</param>
    public static DbContextOptionsBuilder UseAbsoluteDateTime(this DbContextOptionsBuilder builder, IDateTimeZoneProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var extension = builder.Options.FindExtension<AbsoluteDateTimeOptionsExtension>()
                        ?? new AbsoluteDateTimeOptionsExtension(provider);

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

        return builder;
    }
}