using Lynx.EfCore;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Local

namespace Lynx.Tests;

public class EfCoreIncludeTests
{
    [Fact]
    public void GetMemberPath()
    {
        EfCoreIncludeHelpers.GetMembers<E1, ICollection<E2>>(e => e.E2s).ShouldBe($"{nameof(E1.E2s)}");
        EfCoreIncludeHelpers.GetMembers<E1, Entity2>(e => e.Entity1!.Entity2)
            .ShouldBe($"{nameof(E1.Entity1)}.{nameof(Entity1.Entity2)}");
        EfCoreIncludeHelpers.GetMembers<E1, ICollection<Child>?>(e => e.Entity1!.Entity2.Children)
            .ShouldBe($"{nameof(E1.Entity1)}.{nameof(Entity1.Entity2)}.{nameof(Entity2.Children)}");
        EfCoreIncludeHelpers.GetMembers<E0, ICollection<E2>?>(e => e.E1!.E2s)
            .ShouldBe($"{nameof(E0.E1)}.{nameof(E1.E2s)}");
    }

    [Fact]
    public void GetIncludePaths()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetIncludePaths))
            .Options;

        using var context = new Context(options);

        context.Set<E1>()

            //Add some unrelated includes
            .Include(e => e.E2s)
            .ThenInclude(x => x.E3s)
            .ThenInclude(x => x.Child)

            //Start chain here
            .Include(e => e.E2s)
            .ThenInclude(x => x.E3s)
            .ThenInclude(x => x.E4s)
            .GetFullIncludePath()

            .ShouldBe($"{nameof(E1.E2s)}.{nameof(E2.E3s)}.{nameof(E3.E4s)}");
    }

    [Fact]
    public void GetIncludePropertiesNested()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetIncludePropertiesNested))
            .Options;

        using var context = new Context(options);

        var entity2Include =IncludeRelatedEntities.GetIncludeProperties(context.Model, typeof(Entity2))
            .Select(p => $"{nameof(E2.Entity2)}.{p}")
            .Prepend(nameof(E2.Entity2));

        var includeMembers = context.Set<E0>()
            .AsNoTracking()
            .Include(e => e.E1!.E2s)
            .GetFullIncludeMembers()
            .ToList();

        // Nested includes should exclude cyclic references
        includeMembers.Select(m => m.Name).Should().BeEquivalentTo(nameof(E0.E1), nameof(E1.E2s));
        IncludeRelatedEntities.GetIncludeProperties(context.Model, typeof(E0), includeMembers).Should().BeEquivalentTo(entity2Include,
            "Nested includes should exclude cyclic references");

        context.Set<E0>()
            .AsNoTracking()
            .Include(e => e.E1!.E2s).ThenIncludeAllReferenced()
            .ShouldBeEmpty();
    }

    [Fact]
    public void GetIncludePathsWithSubExpression()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetIncludePathsWithSubExpression))
            .Options;

        using var context = new Context(options);

        context.Set<E1>()
            .Include(e => e.E2s.OrderByDescending(x => x.Id))
            .ThenInclude(e => e.E3s)
            .ThenInclude(e => e.E4s.Where(x => x.Id == 0).OrderBy(x => x.Id))
            .GetFullIncludePath()

            .ShouldBe($"{nameof(E1.E2s)}.{nameof(E2.E3s)}.{nameof(E3.E4s)}");
    }

    [Fact]
    public void ThenIncludeAllReferencedShouldExcludeParent()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(ThenIncludeAllReferencedShouldExcludeParent))
            .Options;

        using var context = new Context(options);

        context.Set<E1>()
            .AsNoTracking()

            .Include(e => e.E2s.OrderBy(x => x.Id).Take(5))
            .ThenIncludeAllReferenced()

            .ShouldBeEmpty(); //Should not result in a cyclic reference error
    }

    private class Context(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<E0>();
            modelBuilder.Entity<E1>();
            modelBuilder.Entity<E2>();
            modelBuilder.Entity<E3>();
            modelBuilder.Entity<E4>();
        }
    }

    private class E0Base : EntityBase
    {
        public int? E1Id { get; set; }
        public E1? E1 { get; set; }
    }

    private class E0 : E0Base; //Derived from E0Base to ensure IncludeRelatedEntities works with derived types

    private class E1 : EntityBase
    {
        public E0? E0 { get; set; }

        public required ICollection<E2> E2s { get; set; }

        public Entity1? Entity1 { get; set; }
    }

    private class E2 : EntityBase
    {
        public required ICollection<E3> E3s { get; set; }

        public Entity2? Entity2 { get; set; }

        public E1? Parent { get; set; }
    }

    private class E3 : EntityBase
    {
        public required Child Child { get; set; }

        public required ICollection<E4> E4s { get; set; }
    }

    private class E4 : EntityBase
    {
        public required Child Child { get; set; }
    }
}