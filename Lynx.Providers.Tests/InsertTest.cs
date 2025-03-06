using Lynx.Providers.Tests.Sqlite;

namespace Lynx.Providers.Tests;

public class InsertTest
{
    [Fact]
    public void Insert()
    {
        using var harness = new SqliteTestHarness();
        using (var context = harness.CreateContext())
        {
            context.Database.EnsureCreated();

            context.Customers.Add(Customer.New(1) with { Tags = null});
            context.SaveChanges();
        }
        using (var context = harness.CreateContext())
        {
            var customer = context.Customers.Single();
            customer.Tags.Should().BeNull();
        }
    }
}