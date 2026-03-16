using JetBrains.Annotations;
using Lynx.DocumentStore.Providers;
using Lynx.Provider.Npgsql;
using Lynx.Provider.Sqlite;
using Lynx.Provider.SqlServer;
using Lynx.Providers.Tests.Npgsql;
using Lynx.Providers.Tests.SqlServer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
// ReSharper disable AccessToDisposedClosure

namespace Lynx.Providers.Tests;

public class LynxProviderFactoryTests
{
    [Fact]
    public void NonExistentProviderShouldThrow()
    {
        const string provider = "Non-existent-provider";
        var ex = Assert.Throws<NotSupportedException>(() => LynxProviderFactory.CreateProvider(provider));
        ex.Message.ShouldContain(provider);
    }

    [Fact]
    public void Sqlite()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        using var context = new MinimalContext(b => b.UseSqlite(conn));
        LynxProviderFactory.GetProvider(context)
            .GetService<TestEntity>()
            .ShouldBeOfType<SqliteLynxEntityService<TestEntity>>();
    }

    [Fact]
    public void SqlServer()
    {
        const string connString = $"{SqlServerTestHarness.ConnString};Initial Catalog={nameof(LynxProviderFactoryTests)}";
        using var context = new MinimalContext(b => b.UseSqlServer(connString));
        LynxProviderFactory.GetProvider(context)
            .GetService<TestEntity>()
            .ShouldBeOfType<SqlServerLynxEntityService<TestEntity>>();
    }

    [Fact]
    public void Npgsql()
    {
        const string connString = $"{NpgsqlTestHarness.ConnString};Database={nameof(LynxProviderFactoryTests)}";
        using var context = new MinimalContext(b => b.UseNpgsql(connString));
        LynxProviderFactory.GetProvider(context)
            .GetService<TestEntity>()
            .ShouldBeOfType<NpgsqlLynxEntityService<TestEntity>>();
    }

    private class MinimalContext : DbContext
    {
        public MinimalContext(Action<DbContextOptionsBuilder> configure)
            : base(BuildOptions(configure))
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>();
        }

        private static DbContextOptions BuildOptions(Action<DbContextOptionsBuilder> configure)
        {
            var builder = new DbContextOptionsBuilder<MinimalContext>();
            configure(builder);
            return builder.Options;
        }
    }

    [PublicAPI]
    private class TestEntity
    {
        public int Id { get; set; }
    }
}