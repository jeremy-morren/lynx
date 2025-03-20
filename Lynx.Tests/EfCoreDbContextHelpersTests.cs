using Lynx.EfCore.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Lynx.Tests;

public class EfCoreDbContextHelpersTests
{
    [Fact]
    public void GetDbContext()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var sqliteOptions = new DbContextOptionsBuilder()
            .UseSqlite(conn)
            .Options;

        var inMemOptions = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetDbContext))
            .Options;

        //Test with 2 different providers (i.e. 2 different QueryContextFactory types)

        using (var context = new TestContext(sqliteOptions))
        {
            context.Set<Entity1>().GetDbContext().ShouldBe(context);
            context.Set<Entity1>().AsNoTracking().GetDbContext().ShouldBe(context);
        }

        using (var context = new TestContext(inMemOptions))
        {
            context.Set<Entity1>().GetDbContext().ShouldBe(context);
            context.Set<Entity1>().AsNoTracking().GetDbContext().ShouldBe(context);
        }
    }
}