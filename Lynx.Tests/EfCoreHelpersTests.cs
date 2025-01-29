using Lynx.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Tests;

public class EfCoreHelpersTests
{
    [Fact]
    public void IncludeEntitiesShouldRecurse()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        
        var options = new DbContextOptionsBuilder()
            .UseSqlite(conn)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();
            
            context.Set<Entity1>().AddRange(Entity1.New(1), Entity1.New(2));
            context.Set<Alone>().AddRange(Alone.New(1), Alone.New(2));
            context.SaveChanges();
        }
        
        using (var context = new TestContext(options))
        {
            var e1 = context.Set<Entity1>().IncludeAllReferenced();
            e1.Should().HaveCount(2);
            Assert.All(e1, e =>
            {
                var e2 = e.Entity2.ShouldNotBeNull();
                e2.Parent.ShouldBe(e);
                e2.Entity3.ShouldNotBeNull();
                e2.Children.ShouldBeNull("Collections should not be included");
                e2.Entity3.Owned1.ShouldNotBeNull();
                e2.Entity3.OwnedList.ShouldNotBeNull();
            });
            
            var e2 = context.Set<Entity2>().IncludeAllReferenced();
            e2.Should().HaveCount(2);
            Assert.All(e2, e =>
            {
                e.Parent.ShouldNotBeNull();
                e.Entity3.ShouldNotBeNull();
                e.Children.ShouldBeNull();
                e.Entity3.OwnedList.ShouldHaveSingleItem();
            });

            var e3 = context.Set<Entity3>().IncludeAllReferenced().ToList();
            e3.Should().HaveCount(2);
            
            var alone = context.Set<Alone>().IncludeAllReferenced();
            alone.Should().HaveCount(2);
        }
    }

    [Fact]
    public void GetIncludePropertiesShouldRecurse()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetIncludePropertiesShouldRecurse))
            .Options;

        using var context = new TestContext(options);
        var model = context.Model;

        IncludeRelatedEntities.GetIncludeProperties(model, typeof(Entity1))
            .Should().BeEquivalentTo(nameof(Entity1.Entity2), $"{nameof(Entity1.Entity2)}.{nameof(Entity2.Entity3)}");
        IncludeRelatedEntities.GetIncludeProperties(model, typeof(Entity2))
            .Should().BeEquivalentTo(nameof(Entity2.Parent), nameof(Entity2.Entity3));
        IncludeRelatedEntities.GetIncludeProperties(model, typeof(Entity3)).ShouldBeEmpty();

        IncludeRelatedEntities.GetIncludeProperties(model, typeof(Alone)).ShouldBeEmpty();
    }
    
    [Fact]
    public void IncludeShadowPropertiesShouldBeIgnored()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(IncludeShadowPropertiesShouldBeIgnored))
            .Options;

        using var context = new TestContext(options);
        var model = context.Model;
        
        IncludeRelatedEntities.GetNavigations(model, typeof(Entity2), null)
            .Select(e => e.Name)
            .Should().BeEquivalentTo(nameof(Entity2.Entity3), nameof(Entity2.Parent));
        
        IncludeRelatedEntities.GetNavigations(model, typeof(Entity1), null)
            .Select(e => e.Name)
            .Should().BeEquivalentTo(nameof(Entity1.Entity2));
        
        IncludeRelatedEntities.GetNavigations(model, typeof(Entity3), null).ShouldBeEmpty();
    }

    [Fact]
    public void GetReferencingEntities()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetReferencingEntities))
            .Options;

        using var context = new TestContext(options);
        var model = context.Model;

        //Entity1: referenced by Entity2 and itself
        model.GetReferencingEntities(typeof(Entity1))
            .Select(e => e.ClrType)
            .Should().BeEquivalentTo([typeof(Entity1), typeof(Entity2)]);
        
        //Entity2: referenced by Entity1
        model.GetReferencingEntities(typeof(Entity2))
            .Select(e => e.ClrType)
            .Should().BeEquivalentTo([typeof(Entity1)]);
        
        //Entity3 and Child: referenced by Entity1 and Entity2
        model.GetReferencingEntities(typeof(Entity3))
            .Select(e => e.ClrType)
            .Should().BeEquivalentTo([typeof(Entity1), typeof(Entity2)]);
        
        //Alone: not referenced by any entity
        model.GetReferencingEntities(typeof(Alone)).ShouldBeEmpty();

        //Child: Referenced as a collection by Entity2
        model.GetReferencingEntities(typeof(Child))
            .Select(e => e.ClrType)
            .Distinct()
            .Should().BeEquivalentTo([typeof(ParentEntity), typeof(Entity1), typeof(Entity2)]);

        //OwnedForeign: referenced by Owned, which is owned by Entity3
        //NB: Owned itself is not an entity, so it should not be included
        model.GetReferencingEntities(typeof(OwnedForeign))
            .Select(e => e.ClrType)
            .Should().BeEquivalentTo([typeof(Entity1), typeof(Entity2), typeof(Entity3), typeof(ParentEntity)]);
    }

    [Fact]
    public void GetEntityIdValue()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetEntityIdValue))
            .Options;

        using var context = new TestContext(options);
        var model = context.Model;

        var x = Entity1.New(5);

        model.GetEntityKey(x).ShouldBe(5);
        model.GetEntityKey(x.Entity2).ShouldBe(5);
        model.GetEntityKey(x.Entity2.Entity3).ShouldBe(5);

        var alone = Alone.New(6);
        model.GetEntityKey(alone)
            .ShouldBeEquivalentTo(new Dictionary<string, object>()
            {
                {nameof(Alone.Id1), 6},
                {nameof(Alone.Id2), 12}
            });
    }
}