using System.ComponentModel.DataAnnotations.Schema;
using Lynx.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Xunit.Abstractions;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace Lynx.Tests;

public class FullIndexTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Postgres()
    {
        var ct = CancellationToken.None;
        var database = DatabaseNameHelpers.GetDatabaseName();
        await using (var server = new NpgsqlConnection(ForeignKeyTests.ConnString))
        {
            await server.OpenAsync(ct);
            await using var cmd = server.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE); CREATE DATABASE \"{database}\"";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var options = new DbContextOptionsBuilder<IndexContext>()
            .UseNpgsql($"{ForeignKeyTests.ConnString};Database={database}")
            .Options;
        await using var context = new IndexContext(options);

        var script = context.GenerateCreateFullIndexesScript();
        output.WriteLine(script);

        // Regular column + complex member (table splitting) + owned member
        script.ShouldContain("""CREATE INDEX IF NOT EXISTS "IX_Runs_FactoryId_StartedAt_Instant_Owned_Code" ON "Runs" ("FactoryId", "StartedAt_Instant", "Owned_Code");""");
#if NET10_0_OR_GREATER
        // Regular column + members inside a JSON column (an expression index, as EF Core translates the member)
        script.ShouldContain("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Custom" ON "Runs" ("FactoryId", ("Details" ->> 'Notes'), (CAST("Details" ->> 'Status' AS integer)));""");
#endif

        await context.Database.EnsureCreatedAsync(ct);
        await FullIndexHelpers.ExecuteCreateFullIndexesAsync(context, ct);
        await FullIndexHelpers.ExecuteCreateFullIndexesAsync(context, ct); // Idempotent

        // The indexes are used by the queries they are for
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.ExecuteSqlRawAsync("SET LOCAL enable_seqscan = off", ct);

        var instant = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var plan = await Explain(context, context.Runs
            .Where(r => r.FactoryId == "F1" && r.StartedAt.Instant > instant)
            .Select(r => r.Id));
        plan.ShouldContain("IX_Runs_FactoryId_StartedAt_Instant_Owned_Code");

#if NET10_0_OR_GREATER
        plan = await Explain(context, context.Runs
            .Where(r => r.FactoryId == "F1" && r.Details.Notes == "N" && r.Details.Status == 1)
            .Select(r => r.Id));
        plan.ShouldContain("IX_Custom");
#endif
    }

    [Fact]
    public async Task Sqlite()
    {
        var ct = CancellationToken.None;
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<IndexContext>().UseSqlite(connection).Options;
        await using var context = new IndexContext(options);

        await context.Database.EnsureCreatedAsync(ct);
        await FullIndexHelpers.ExecuteCreateFullIndexesAsync(context, ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND name IN ('IX_Runs_FactoryId_StartedAt_Instant_Owned_Code', 'IX_Custom')";
        var names = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            names.Add(reader.GetString(0));
#if NET10_0_OR_GREATER
        names.Count.ShouldBe(2);
#else
        names.Count.ShouldBe(1);
#endif
    }

    private async Task<string> Explain<T>(DbContext context, IQueryable<T> query)
    {
        await using var cmd = query.CreateDbCommand();
        cmd.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = $"EXPLAIN {cmd.CommandText}";
        var plan = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            plan.Add(reader.GetString(0));
        var result = string.Join(Environment.NewLine, plan);
        output.WriteLine(result);
        return result;
    }

    private class IndexContext(DbContextOptions<IndexContext> options) : DbContext(options)
    {
        public DbSet<Run> Runs => Set<Run>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Run>(b =>
            {
                b.ComplexProperty(x => x.StartedAt);
                b.OwnsOne(x => x.Owned);
#if NET10_0_OR_GREATER
                b.ComplexProperty(x => x.Details, d => d.ToJson());
                b.HasFullIndex(x => new { x.FactoryId, x.Details.Notes, x.Details.Status }, "IX_Custom", unique: true);
#else
                b.Ignore(x => x.Details);
#endif
                b.HasFullIndex(x => new { x.FactoryId, x.StartedAt.Instant, x.Owned.Code });
            });
        }
    }

    private class Run
    {
        public int Id { get; init; }
        public required string FactoryId { get; init; }
        public required Stamp StartedAt { get; init; }
        public required Details Details { get; init; }
        public required OwnedPart Owned { get; init; }
    }

    [ComplexType]
    private class Stamp
    {
        public required string Value { get; init; }
        public DateTime Instant { get; init; }
    }

    private class Details
    {
        public required string Notes { get; init; }
        public int Status { get; init; }
    }

    private class OwnedPart
    {
        public required string Code { get; init; }
    }
}
