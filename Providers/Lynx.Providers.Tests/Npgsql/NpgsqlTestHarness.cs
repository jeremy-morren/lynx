using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lynx.Providers.Tests.Npgsql;

public sealed class NpgsqlTestHarness : IDisposable
{
    private readonly string _database;

    public NpgsqlTestHarness(params string[] database)
    {
        _database = string.Join('_', database).ToLowerInvariant();
        DeleteDatabase(_database);
    }

    public TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseNpgsql($"{ConnString};Database={_database}", o => o.UseNodaTime())
            .Options;
        
        return new TestContext(options);
    }
    
    public void Dispose() => DeleteDatabase(_database);

    private const string ConnString = "Host=localhost;Username=postgres;Password=postgres;Include Error Detail=true";

    private static void DeleteDatabase(string dbName) => ExecuteNonQuery($"DROP DATABASE IF EXISTS {dbName}");

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