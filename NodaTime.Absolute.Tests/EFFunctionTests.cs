using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NodaTime.Absolute.EFCore;
using NodaTime.Extensions;
using Xunit.Abstractions;

namespace NodaTime.Absolute.Tests;

public class EFFunctionTests(ITestOutputHelper output)
{
    [Fact]
    public void EnableAbsoluteTimeOnDbContextShouldSucceed()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(connection, x => x.UseNodaTime())
            .UseAbsoluteDateTime()
            .Options;

        using var context = new SqliteDbContext(options);

        output.WriteLine(context.Database.GenerateCreateScript());
        context.Database.EnsureCreated();

        var instant = SystemClock.Instance.GetCurrentInstant();
        const string zone = "Australia/Sydney";
        context.Add(DateEntity.New(instant, zone));
        context.Add(DateEntity.New(instant, zone));
        context.SaveChanges();

        var query =
            from d in context.Set<DateEntity>().AsNoTracking()
            select d.Owned.Date;

        output.WriteLine(query.ToQueryString());
        query.ToList().Should().HaveCount(2)
            .And.AllSatisfy(d =>
            {
                d.ToInstant().Should().Be(instant);
                d.Zone.Id.Should().Be(zone);
            });
    }

    [Fact]
    public void QueryWithFunctionsShouldSucceed()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(connection, x => x.UseNodaTime())
            .UseAbsoluteDateTime()
            .Options;

        using var context = new SqliteDbContext(options);

        output.WriteLine(context.Database.GenerateCreateScript());
        context.Database.EnsureCreated();

        var now = SystemClock.Instance.GetCurrentInstant();
        const string zone = "Australia/Sydney";
        context.Add(DateEntity.New(now, zone));
        context.Add(DateEntity.New(now, zone));
        context.SaveChanges();

        context.Set<DateEntity>().AsNoTracking()
            .Where(d => d.Owned.Date.ToInstant() < SystemClock.Instance.GetCurrentInstant())
            .Select(d => d.Owned.Date)
            .Log(output)
            .ToList().Should().HaveCount(2)
            .And.AllSatisfy(d =>
            {
                d.ToInstant().Should().Be(now);
                d.Zone.Id.Should().Be(zone);
            });

        context.Set<DateEntity>().AsNoTracking()
            .Where(d => d.Date.GetZoneId() == zone)
            .Select(d => d.Owned.Date.ToInstant())
            .Log(output)
            .ToList()
            .Should().HaveCount(2)
            .And.AllSatisfy(i => i.Should().Be(now));
    }

    private class SqliteDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DateEntity>(b =>
            {
                b.OwnsOne(x => x.Owned, x =>
                {
                    x.HasIndex(y => y.Date);
                    x.ToJson();
                });
            });
        }
    }

    private class DateEntity
    {
        public int Id { get; set; }

        public AbsoluteDateTime Date { get; set; }

        public required Owned Owned { get; set; }

        public static DateEntity New(Instant instant, string zone) => new()
        {
            Date = instant.InZone(DateTimeZoneProviders.Tzdb[zone]),
            Owned = new Owned
            {
                Date = instant.InZone(DateTimeZoneProviders.Tzdb[zone])
            }
        };
    }

    private class Owned
    {
        public AbsoluteDateTime Date { get; set; }
    }
}