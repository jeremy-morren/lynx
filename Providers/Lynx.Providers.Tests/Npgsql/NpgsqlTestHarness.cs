using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Lynx.Providers.Tests.Npgsql;

public sealed class NpgsqlTestHarness : ITestHarness
{
    private readonly string _database;

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlTestHarness(object[] database, bool enableNodaTimeOnDataSource = false)
    {
        _database = string.Join('_', database).ToLowerInvariant();
        DeleteDatabase(_database);

        var builder = new NpgsqlDataSourceBuilder($"{ConnString};Database={_database}");
        builder.EnableDynamicJson();
        if (enableNodaTimeOnDataSource)
            builder.UseNodaTime();
        _dataSource = builder.Build();
    }

    public TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseNpgsql(_dataSource, o => o.UseNodaTime())
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        
        return new TestContext(options);
    }
    
    public void Dispose()
    {
        _dataSource.Dispose();
        DeleteDatabase(_database);
    }

    public const string ConnString = "Host=localhost;Username=postgres;Password=postgres;Include Error Detail=true";

    private static void DeleteDatabase(string dbName)
    {
        ArgumentException.ThrowIfNullOrEmpty(dbName);
        ExecuteNonQuery($"DROP DATABASE IF EXISTS \"{dbName}\"");
    }

    private static void ExecuteNonQuery(string command)
    {
        using var conn = new NpgsqlConnection(ConnString);
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = command;
        cmd.ExecuteNonQuery();
    }
}