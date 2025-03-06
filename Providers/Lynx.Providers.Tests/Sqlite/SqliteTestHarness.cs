using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lynx.Providers.Tests.Sqlite;

public sealed class SqliteTestHarness : IDisposable
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
            .UseSqlite(Connection, x => x.UseNodaTime())
            .Options;

        return new TestContext(options);
    }

    public void Dispose()
    {
        Connection.Dispose();
    }
}