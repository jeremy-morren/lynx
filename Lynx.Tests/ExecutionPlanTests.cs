using Lynx.ExecutionPlan;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace Lynx.Tests;

public class ExecutionPlanTests(ITestOutputHelper output)
{
    [Fact]
    public void GetExecutionPlanSqlite()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(conn)
            .Options;

        using var context = new ComplexContext(options);
        context.Database.EnsureCreated();

        var plan = context.Query<Parent>()
            .Where(p => p.Foreign.Id == 4)
            .Where(p => p.Children.Any(c => c.Id == 4))
            .GetExecutionPlan();
        plan.ShouldNotBeNull();
        plan.Nodes.ShouldNotBeEmpty();
        plan.CommandText.ShouldNotContain("EXPLAIN");

        output.WriteLine(plan.DebugView);
        output.WriteLine(plan.CommandText);
    }

    [Fact]
    public void GetExecutionPlanNpgsql()
    {
        const string connStr = "Host=localhost;Username=postgres;Password=postgres";
        const string dbName = "lynx_execution_plan_tests";

        DropDatabase();

        try
        {
            ExecuteNonQuery($"CREATE DATABASE {dbName}");

            var options = new DbContextOptionsBuilder()
                .UseNpgsql($"{connStr};Database={dbName}")
                .Options;

            using var context = new ComplexContext(options);
            context.Database.EnsureCreated();

            var plan = context.Query<Parent>()
                .Where(p => p.Foreign.Id == 4)
                .Where(p => p.Children.Any(c => c.Id == 4))
                .GetExecutionPlan();
            plan.ShouldNotBeNull();
            plan.Nodes.ShouldNotBeEmpty();
            plan.CommandText.ShouldNotContain("EXPLAIN");

            output.WriteLine(plan.DebugView);
            output.WriteLine(plan.CommandText);

        }
        finally
        {
            DropDatabase();
        }

        return;

        void ExecuteNonQuery(string sql)
        {
            using var connection = new NpgsqlConnection(connStr);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        void DropDatabase()
        {
            try
            {
                ExecuteNonQuery($"DROP DATABASE \"{dbName}\" WITH (FORCE)");
            }
            catch (NpgsqlException e) when (e.SqlState == "3D000")
            {
                // Database doesn't exist, ignore
            }
        }
    }

    private class ComplexContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent>();
        }

        public IQueryable<T> Query<T>() where T : class => Set<T>().AsNoTracking();
    }

    private class Parent
    {
        public required int Id { get; init; }

        public required Foreign Foreign { get; init; }

        public required Child[] Children { get; init; }
    }

    private class Foreign
    {
        public required int Id { get; init; }
    }

    private class Child
    {
        public required int Id { get; init; }
    }
}