using System.Linq.Expressions;
using Lynx.DocumentStore;
using Lynx.DocumentStore.Query;
using Lynx.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;


// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Tests;

public class DocumentStoreQueryTests
{
    [Fact]
    public void DocumentStoreQueryShouldIncludeRelatedEntities()
    {
        using var _ = TestContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var entity = ParentEntity.Create(1, childId: 1);
            context.Add(entity);
            context.SaveChanges();
        }
        using (var context = factory())
        {
            context.Query<ParentEntity>().ShouldNotBeEmpty();
            context.Query<ParentEntity>().ShouldAllBe(e => e.Child == null);

            var store = new DocumentStore<TestContext>(context);

            store.Query<ParentEntity>().AsNoTracking().GetDbContext().ShouldBe(context);

            store.Query<ParentEntity>().ShouldAllBe(e => e.Child != null);
        }
    }

    [Fact]
    public async Task Load()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(conn)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();

            context.Add(Entity1.New(1));
            context.Add(Alone.New(2));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            var store = new DocumentStore<TestContext>(context);

            store.Get<Entity1>(1).ShouldNotBeNull().Entity2.ShouldNotBeNull();
            (await store.LoadAsync<Entity1>(1)).ShouldNotBeNull().Entity2.ShouldNotBeNull();

            store.Get<Entity1>(2).ShouldBeNull();
            (await store.LoadAsync<Entity1>(2)).ShouldBeNull();

            store.Get<Alone>(new { Id1 = 2, Id2 = 4 }).ShouldNotBeNull();
            (await store.LoadAsync<Alone>(new { Id1 = 2, Id2 = 4 })).ShouldNotBeNull();

            store.Get<Alone>(new { Id1 = 2, Id2 = 3 }).ShouldBeNull();
            (await store.LoadAsync<Alone>(new { Id1 = 2, Id2 = 3 })).ShouldBeNull();
        }
    }

    [Fact]
    public void UseSoftDelete()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder().UseSqlite(conn).Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();

            context.Add(Entity1.New(1, true));
            context.Add(Alone.New(2));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            context.Set<Entity2>().ShouldNotBeEmpty();

            var store = new DocumentStore<TestContext>(context);

            store.Query<Entity1>().ShouldBeEmpty("Query should not return soft deleted entities");
            store.QueryRaw<Entity1>().ShouldNotBeEmpty("Query raw should return soft deleted entities");

            store.Get<Entity1>(1).ShouldNotBeNull("Get should return soft deleted entities");
            store.Get<Alone>(new { Id1 = 2, Id2 = 4}).ShouldNotBeNull();

            store.Query<Entity2>().ToList()
                .Should().HaveCount(1)
                .And.AllSatisfy(e => e.Parent.ShouldNotBeNull(),
                    "Soft delete should not affect foreign references");
        }
    }
}