using Lynx.EfCore;
using Lynx.EfCore.Helpers;
using Microsoft.Data.Sqlite;
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
    public void GetIncludePathsWithSubExpression()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(nameof(GetIncludePaths))
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
            modelBuilder.Entity<E1>();
        }
    }
    private class E1 : EntityBase
    {
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