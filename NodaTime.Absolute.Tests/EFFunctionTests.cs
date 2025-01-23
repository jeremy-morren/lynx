using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
            .Options;

        using var context = new SqliteDbContext(options);


        output.WriteLine(context.Database.GenerateCreateScript());

        var localDate = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime.Date;

        var query =
            from d in context.Set<DateEntity>()
            where d.Instant < SystemClock.Instance.GetCurrentInstant()
                  && d.Owned.Date < localDate
            select d.Owned.Date;

        output.WriteLine(query.ToQueryString());

        context.Database.EnsureCreated();
        query.Should().BeEmpty();
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

        public Instant Instant { get; set; }

        public required Owned Owned { get; set; }
    }

    private class Owned
    {
        public LocalDate Date { get; set; }
    }
}