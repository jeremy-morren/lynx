using Lynx.DocumentStore;
using Lynx.DocumentStore.Query;
using Lynx.EfCore;
using Lynx.EfCore.Helpers;
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
    public void FilterByIds()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder()
            .UseSqlite(conn)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();

            context.Add(Entity1.New(1, deleted: true));
            context.Add(Entity1.New(2, deleted: true));
            context.Add(Alone.New(1));
            context.Add(Alone.New(2));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            context.Query<Entity1>().FilterByIds([1, 2]).Should().HaveCount(2);
            context.Query<Entity1>().FilterByIds([1, 1, 2]).Should().HaveCount(2);
            context.Query<Entity1>().FilterByIds([2, 3]).ShouldHaveSingleItem();

            context.Query<Alone>()
                .FilterByIds([new { Id1 = 1, Id2 = 2}, new { Id1 = 2, Id2 = 4}])
                .Should().HaveCount(2);
            context.Query<Alone>()
                .FilterByIds([new { Id1 = 1, Id2 = 2 }, new { Id1 = -1, Id2 = -1 }])
                .ShouldHaveSingleItem();
            context.Query<Alone>()
                .FilterByIds([new { Id1 = -1, Id2 = -1 }])
                .ShouldBeEmpty();

            context.Query<Entity1>().FilterByIds(Enumerable.Empty<int>()).ShouldBeEmpty();
            context.Query<Alone>().FilterByIds(Enumerable.Empty<int>()).ShouldBeEmpty();
            context.Query<Alone>()
                .FilterByIds(new [] { new { Id1 = 1, Id2 = 2 } }.Take(0))
                .ShouldBeEmpty();

            //Test dictionary keys
            var keys = context.Query<Alone>().AsEnumerable().Select(context.Model.GetEntityKey).ToList();
            context.Query<Alone>().FilterByIds(keys).Should().HaveCount(keys.Count);
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

        const int id = 1;
        
        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();

            context.Add(Entity1.New(id, deleted: true));
            context.Add(Alone.New(2));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            var store = new DocumentStore<TestContext>(context);

            store.Load<Entity1>(id).ShouldNotBeNull().Entity2.ShouldNotBeNull();
            (await store.LoadAsync<Entity1>(id)).ShouldNotBeNull().Entity2.ShouldNotBeNull();
            store.Load<Entity1>(id, true).ShouldNotBeNull().Entity2.ShouldNotBeNull();
            (await store.LoadAsync<Entity1>(id, true)).ShouldNotBeNull().Entity2.ShouldNotBeNull();
            
            store.Load<Entity1>(id, false).ShouldBeNull();
            (await store.LoadAsync<Entity1>(id, false)).ShouldBeNull();

            store.Load<Entity1>(2).ShouldBeNull();
            (await store.LoadAsync<Entity1>(2)).ShouldBeNull();

            store.Load<Entity1>(2, true).ShouldBeNull();
            (await store.LoadAsync<Entity1>(2, true)).ShouldBeNull();

            store.Load<Alone>(new { Id1 = 2, Id2 = 4 }).ShouldNotBeNull();
            (await store.LoadAsync<Alone>(new { Id1 = 2, Id2 = 4 })).ShouldNotBeNull();
            store.Load<Alone>(new { Id1 = 2, Id2 = 4 }, true).ShouldNotBeNull();
            (await store.LoadAsync<Alone>(new { Id1 = 2, Id2 = 4 }, false)).ShouldNotBeNull();

            store.Load<Alone>(new { Id1 = 2, Id2 = 3 }).ShouldBeNull();
            (await store.LoadAsync<Alone>(new { Id1 = 2, Id2 = 3 })).ShouldBeNull();
            store.Load<Alone>(new { Id1 = 2, Id2 = 3 }, false).ShouldBeNull();
            (await store.LoadAsync<Alone>(new { Id1 = 2, Id2 = 3 }, false)).ShouldBeNull();
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

            store.Load<Entity1>(1).ShouldNotBeNull("Load should return soft deleted entities");
            store.Load<Entity1>(1, true).ShouldNotBeNull();
            store.Load<Entity1>(1, false).ShouldBeNull();

            store.Load<Alone>(new { Id1 = 2, Id2 = 4}).ShouldNotBeNull();
            store.Query<Alone>().Should().HaveCount(1);

            store.Query<Entity2>().ToList()
                .Should().HaveCount(1)
                .And.AllSatisfy(e => e.Parent.ShouldNotBeNull(),
                    "Soft delete should not affect foreign references");
        }
    }

    [Fact]
    public void FilterByStrongId()
    {
        //Use Npgsql, where EF.Property<object> doesn't work with StrongId

        const string connString = "Host=localhost;Port=5432;Database=lynx;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder()
            .UseNpgsql(connString)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Set<EntityStrongId>().AddRange(Enumerable.Range(0,10).Select(EntityStrongId.New));
            context.Set<EntityStrongIdComposite>().AddRange(Enumerable.Range(0,10).Select(EntityStrongIdComposite.New));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            var store = new DocumentStore<TestContext>(context);

            store.Load<EntityStrongId>(new StrongId(1)).ShouldNotBeNull();
            store.Load<EntityStrongId>(new StrongId(-1)).ShouldBeNull();

            store.Query<EntityStrongId>()
                .FilterByIds(Enumerable.Range(0, 4).Select(i => new StrongId(i)))
                .Should()
                .HaveCount(4);

            store.Load<EntityStrongIdComposite>(new { Id1 = new StrongId(1), Id2 = new StrongId(2)}).ShouldNotBeNull();
            store.Load<EntityStrongIdComposite>(new { Id1 = new StrongId(1), Id2 = new StrongId(1)}).ShouldBeNull();

            store.Query<EntityStrongIdComposite>()
                .FilterByIds(Enumerable.Range(0, 4)
                    .Select(i => new { Id1 = new StrongId(i), Id2 = new StrongId(i * 2) }))
                .Should()
                .HaveCount(4);
        }
    }
}