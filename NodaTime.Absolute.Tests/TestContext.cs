using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NodaTime.Absolute.Tests;


public class TestContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>();

        modelBuilder.Entity<DateEntity>(b =>
        {
            b.OwnsOne(x => x.Owned, x =>
            {
                x.ToJson();

                x.HasIndex(y => y.Date);
                x.OwnsOne(y => y.Owned2);
            });
        });
    }
}

public class DateEntity
{
    public int Id { get; set; }

    public AbsoluteDateTime Date { get; set; }

    public required Owned Owned { get; set; }

    public static DateEntity New(Instant instant, string zone) => new()
    {
        Date = instant.InZone(DateTimeZoneProviders.Tzdb[zone]),
        Owned = new Owned
        {
            Date = instant.InZone(DateTimeZoneProviders.Tzdb[zone]),
            Owned2 = new Owned
            {
                Date = instant.InZone(DateTimeZoneProviders.Tzdb[zone])
            }
        }
    };
}

public class Owned
{
    public AbsoluteDateTime Date { get; set; }

    public Owned? Owned2 { get; set; }
}

public class Person
{
    public int Id { get; set; }

    public required JsonElement Json { get; set; }
}