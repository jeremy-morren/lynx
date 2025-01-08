using Lynx.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
            var e1 = context.Set<Entity1>().IncludeAll();
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
            
            var e2 = context.Set<Entity2>().IncludeAll();
            e2.Should().HaveCount(2);
            Assert.All(e2, e =>
            {
                e.Parent.ShouldNotBeNull();
                e.Entity3.ShouldNotBeNull();
                e.Children.ShouldBeNull();
                e.Entity3.OwnedList.ShouldHaveSingleItem();
            });

            var e3 = context.Set<Entity3>().IncludeAll().ToList();
            e3.Should().HaveCount(2);
            
            var alone = context.Set<Alone>().IncludeAll();
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

        //Entity1: referenced by Entity2

        model.GetReferencingEntities(typeof(Entity1))
            .Select(e => e.ClrType)
            .Should().BeEquivalentTo([typeof(Entity2)]);
        
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
                {nameof(Alone.Id2), 6}
            });
    }

    private class TestContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entity1>()
                .HasOne(e => e.Entity2)
                .WithOne(e => e.Parent)
                .HasForeignKey((Entity2 e) => e.ParentId);

            modelBuilder.Entity<Entity3>(b =>
            {
                b.OwnsOne(x => x.Owned1);
                b.OwnsMany(x => x.OwnedList);
            });

            modelBuilder.Entity<Alone>()
                .HasKey(x => new { x.Id1, x.Id2 });
        }
    }

    private class EntityBase
    {
        public required int Id { get; init; }
    }
    
    private class Entity1 : EntityBase
    {
        public required Entity2 Entity2 { get; init; }

        public static Entity1 New(int id) => new()
        {
            Id = id,
            Entity2 = new Entity2()
            {
                Parent = null!,
                Id = id,
                Entity3 = new Entity3()
                {
                    Id = id, 
                    Owned1 = new Owned() { Id = id},
                    OwnedList = new List<Owned> { new () { Id = id } }
                },
                Children = new List<Child>
                {
                    new() { Id = id }
                }
            }
        };
    }

    private class Entity2 : EntityBase
    {
        public required Entity3 Entity3 { get; init; }
        
        public ICollection<Child>? Children { get; init; }
        
        public int ParentId { get; init; }
        public required Entity1 Parent { get; init; }
    }

    private class Entity3 : EntityBase
    {
        public required Owned Owned1 { get; init; }
        public required ICollection<Owned> OwnedList { get; init; }
    }
    
    private class Owned : EntityBase
    {
    }
    
    private class Child : EntityBase {}

    private class Alone
    {
        public required int Id1 { get; init; }

        public int Id2
        {
            get => Id1;
            // EF Core requires a setter
            // ReSharper disable once ValueParameterNotUsed
            init {}
        }
        
        public static Alone New(int id) => new() { Id1 = id };
    }
}