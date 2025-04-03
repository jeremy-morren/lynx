using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Lynx.Tests;

public class EfCoreConcatManyTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(100)]
    [InlineData(505)]
    [InlineData(1005)]
    public void ConcatManyQueriesShouldSucceed(int count)
    {
        var connString = $"Host=localhost;Database={nameof(ConcatManyQueriesShouldSucceed)}_{count};Username=postgres;Password=postgres;";

        var options = new DbContextOptionsBuilder<TestContext>()
            .UseNpgsql(connString)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Set<Entity1>().AddRange(Enumerable.Range(10, count).Select(i => Entity1.New(i)));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            context.Set<Entity1>().AsNoTracking().Should().HaveCount(count);

            var ids = Enumerable.Range(10, count).OrderBy(_ => Random.Shared.Next()).ToList();
            var queries = ids
                .Select(i => context.Set<Entity1>()
                    .Where(e => e.Id == i)
                    .Select(e => e.Id)
                    .Take(1))
                .ToList();

            var result = queries.ConcatMany().ToList();
            result.Should().HaveCount(count)
                .And.BeEquivalentTo(ids);

            context.Database.EnsureDeleted(); // Cleanup
        }
    }
}