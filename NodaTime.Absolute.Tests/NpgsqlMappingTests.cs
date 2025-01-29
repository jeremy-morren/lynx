using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NodaTime.Absolute.EFCore;
using NodaTime.Absolute.EFCore.Serialization;
using NodaTime.Extensions;
using Npgsql;
using NpgsqlTypes;
using Xunit.Abstractions;

namespace NodaTime.Absolute.Tests;

public class NpgsqlMappingTests(ITestOutputHelper output)
{
    [Fact]
    public void EnableAbsoluteTimeOnDbContextShouldSucceed()
    {
        var options = new DbContextOptionsBuilder()
            .UseAbsoluteDateTime()
            .UseNpgsql(ConnString, x => x.UseNodaTime())
            .Options;

        using var context = new TestContext(options);

        output.WriteLine(context.Database.GenerateCreateScript());
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var instant = SystemClock.Instance.GetCurrentInstant();
        const string zone = "Australia/Sydney";
        context.Set<DateEntity>().ExecuteDelete();
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

    private const string ConnString = "Host=localhost;Username=postgres;Password=postgres;Database=AbsoluteDateTimeTests";
}