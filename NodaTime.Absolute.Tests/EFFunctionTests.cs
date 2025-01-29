using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NodaTime.Absolute.EFCore;
using NodaTime.Absolute.EFCore.Serialization;
using NodaTime.Extensions;
using Xunit.Abstractions;

namespace NodaTime.Absolute.Tests;

public class EFFunctionTests(ITestOutputHelper output)
{
    [Fact]
    public void QueryJson()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(connection, x => x.UseNodaTime())
            .UseAbsoluteDateTime()
            .Options;

        using var context = new TestContext(options);

        output.WriteLine(context.Database.GenerateCreateScript());
    }

    [Fact]
    public void EnableAbsoluteTimeOnDbContextShouldSucceed()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(connection, x => x.UseNodaTime())
            .UseAbsoluteDateTime()
            .Options;

        using var context = new TestContext(options);

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


        var query =
            from d in context.Set<DateEntity>().AsNoTracking()
            where d.Date != d.Owned.Date
            orderby d.Date
            select d.Owned.Date;
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

        using var context = new TestContext(options);

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

        context.Set<DateEntity>().AsNoTracking()
            .Where(d => d.Date.GetZoneId() == zone)
            .Select(d => new
            {
                A = d.Date.LocalDateTime,
                B = d.Owned.Date.LocalDateTime,
                C = d.Date.ToInstant()
            })
            .Log(output)
            .ToList()
            .Should().HaveCount(2)
            .And.AllSatisfy(i =>
            {
                var dt = now.InZone(DateTimeZoneProviders.Tzdb[zone]);
                i.A.Should().Be(dt.LocalDateTime);
                i.B.Should().Be(dt.LocalDateTime);
                i.C.Should().Be(now);
            });
    }

    [Fact]
    public void QueryOwnedJsonShouldSucceed()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(connection, x => x.UseNodaTime())
            .UseAbsoluteDateTime()
            .Options;

        using var context = new TestContext(options);

        context.Set<DateEntity>().AsNoTracking()
            .Select(d => d.Owned.Owned2)
            .Log(output);

        // output.WriteLine(context.Database.GenerateCreateScript());
        // context.Database.EnsureCreated();
        //
        // var now = SystemClock.Instance.GetCurrentInstant();
        // const string zone = "Australia/Sydney";
        // context.Add(DateEntity.New(now, zone));
        // context.Add(DateEntity.New(now, zone));
        // context.SaveChanges();

        // var query = context.Set<DateEntity>().AsNoTracking()
        //     .Where(d => d.Date.GetZoneId() == zone)
        //     .Select(d => d.Owned.Owned2!.Date)
        //     .Log(output);
        //
        // using var cmd = query.CreateDbCommand();
        // using var reader = cmd.ExecuteReader();
        // reader.Read().Should().BeTrue();
        // var json = reader.GetString(0);
        // AbsoluteDateTimeJson.FromJson(json, DateTimeZoneProviders.Tzdb).ToInstant().Should().Be(now);
    }
}