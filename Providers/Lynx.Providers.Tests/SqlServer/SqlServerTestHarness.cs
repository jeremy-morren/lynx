using System.Data;
using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lynx.Providers.Tests.SqlServer;

public sealed class SqlServerTestHarness : ITestHarness
{
    private readonly string _database;

    public SqlServerTestHarness(object[] database)
    {
        _database = string.Join('_', database).ToLowerInvariant();

        DeleteDatabase(_database);
    }

    public TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlServer($"{ConnString};Initial Catalog={_database}")
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        
        return new TestContext(options);
    }
    
    public void Dispose()
    {
        DeleteDatabase(_database);
    }

    public const string ConnString =
        "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";

    private static void DeleteDatabase(string dbName)
    {
        ArgumentException.ThrowIfNullOrEmpty(dbName);
        SqlConnection.ClearAllPools();
        try
        {
            ExecuteNonQueryMaster($"DROP DATABASE [{dbName}]");
        }
        catch (SqlException ex) when (ex.Number == 3701) // Cannot drop the database because it does not exist
        {
        }
    }

    private static void ExecuteNonQueryMaster(string command)
    {
        using var conn = new SqlConnection($"{ConnString};Initial catalog=master");
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = command;
        cmd.ExecuteNonQuery();
    }
}