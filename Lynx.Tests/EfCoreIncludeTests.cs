using Lynx.EfCore;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Local

namespace Lynx.Tests;

public class EfCoreIncludeTests
{
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
    }

    private class E3 : EntityBase
    {
        public required Child Child { get; set; }

        public ICollection<E4> E4s { get; set; }
    }

    private class E4 : EntityBase
    {
        public required Child Child { get; set; }
    }
}