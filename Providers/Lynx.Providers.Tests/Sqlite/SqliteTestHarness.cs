using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lynx.Providers.Tests.Sqlite;

public sealed class SqliteTestHarness : ITestHarness
{
    public SqliteConnection Connection { get; }

    public SqliteTestHarness()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        Connection.Open();
    }

    public TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder()
            .UseSqlite(Connection)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new TestContext(options);
    }

    public void Dispose()
    {
        Connection.Dispose();
    }
}