using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.Tests;

public class DistinctOnQueryExtensionsTests
{
    [Fact]
    public void SingleProperty()
    {
        var options = CreateOptions();
        using var context = new TestContext(options);

        var query = context
            .Query<ParentEntity>()
            .SqlDistinctOn(
                e => e.Id,
                e => e.Iteration,
                [false, true],
                e => e.Id);

        using var command = query.CreateDbCommand();
        command.CommandText.ShouldStartWith("SELECT DISTINCT ON (p.\"Id\") ");
        command.CommandText.ShouldEndWith($"ORDER BY p.\"Id\" ASC, p.\"Iteration\" DESC{Environment.NewLine}");

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        query.ShouldBeEmpty();

        query.Where(x => x > 0).ShouldBeEmpty();
    }

    [Fact]
    public void MultipleProperties()
    {
        var options = CreateOptions();
        using var context = new TestContext(options);

        var query = context
            .Query<ParentEntity>()
            .Where(e => e.Child!.ForeignId > 0)
            .SqlDistinctOn(
                e => new { e.Id, e.Iteration },
                e => new { e.Property1, Column = e.Property2 },
                [false, true, true, false],
                e => new SelectorResult()
                {
                    Val1 = e.Id,
                    Val2 = e.Iteration,
                    Val3 = e.Property1,
                });

        using var command = query.CreateDbCommand();
        command.CommandText.ShouldStartWith("SELECT DISTINCT ON (p.\"Id\", p.\"Iteration\") ");
        command.CommandText.ShouldEndWith($"ORDER BY p.\"Id\" ASC, p.\"Iteration\" DESC, p.\"Property1\" DESC, p.\"Property2\" ASC{Environment.NewLine}");

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        query.ShouldBeEmpty();
        query.Where(x => x.Val1 > 0).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Table", "FROM \"Table\" AS \"t\"", "\"t\"")]
    [InlineData("tbl", "FROM \"x\" LEFT JOIN \"schema\".\"Tbl\" as \"t0\"", "\"t0\"")]
    [InlineData("tbl", "FROM \"Tbl\" as t1", "t1")]
    public void GetTableAlias(string tableName, string sql, string alias)
    {
        DistinctOnQueryExtensions.GetTableAlias(tableName, sql).ShouldBe(alias);
    }

    private static DbContextOptions<TestContext> CreateOptions([CallerMemberName] string? database = null)
    {
        database.ShouldNotBeNull();

        var connString = $"Host=localhost;Database={database};Username=postgres;Password=postgres;";

        return new DbContextOptionsBuilder<TestContext>()
            .UseNpgsql(connString)
            .Options;
    }

    [PublicAPI]
    private record SelectorResult
    {
        public required int Val1 { get; init; }

        public required int? Val2 { get; init; }

        public required string? Val3 { get; init; }
    }
}