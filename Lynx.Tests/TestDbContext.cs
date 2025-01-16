using Lynx.DocumentStore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Lynx.Tests;

public class TestContext : DbContext
{
    public TestContext(DbContextOptions options)
        : base(options) {}

    public IQueryable<T> Query<T>() where T : class => Set<T>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParentEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.OwnsOne(x => x.Owned);
        });

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

    /// <summary>
    /// Creates a test context with an in-memory sqlite database
    /// </summary>
    /// <param name="factory"></param>
    /// <returns></returns>
    public static IDisposable CreateContext(out Func<TestContext> factory)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(connection)
            .Options;

        factory = () =>
        {
            var context = new TestContext(options);
            context.Database.EnsureCreated();
            return context;
        };

        return connection;
    }
}

public class EntityBase
{
    public required int Id { get; init; }
}

/// <summary>
/// Parent entity. 1:1 with <see cref="Child"/>
/// </summary>
public class ParentEntity : EntityBase
{
    public required Owned Owned { get; init; }

    public required int? Iteration { get; init; }

    public Child? Child { get; init; }

    public static ParentEntity Create(int id, int? iteration = null, int? childId = null) => new()
    {
        Id = id,
        Iteration = iteration,
        Owned = new Owned() { Id = id },
        Child = childId.HasValue ? new Child() { Id = childId.Value } : null
    };
}

public class Entity1 : EntityBase
{
    public required Entity2 Entity2 { get; init; }

    public required bool Deleted { get; init; }

    public static Entity1 New(int id, bool deleted = false) => new()
    {
        Id = id,
        Deleted = deleted,
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

public class Entity2 : EntityBase
{
    public required Entity3 Entity3 { get; init; }

    public ICollection<Child>? Children { get; init; }

    public int ParentId { get; init; }
    public required Entity1 Parent { get; init; }
}

public class Entity3 : EntityBase
{
    public required Owned Owned1 { get; init; }
    public required ICollection<Owned> OwnedList { get; init; }
}

public class Owned : EntityBase;

public class Child : EntityBase;

/// <summary>
/// Not referenced. Also has composite key
/// </summary>
public class Alone
{
    public required int Id1 { get; init; }

    /// <summary>
    /// <see cref="Id1"/> * 2
    /// </summary>
    public int Id2
    {
        get => Id1 * 2;
        // EF Core requires a setter
        // ReSharper disable once ValueParameterNotUsed
        init {}
    }

    public static Alone New(int id) => new() { Id1 = id };
}