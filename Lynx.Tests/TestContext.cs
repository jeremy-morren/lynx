using JetBrains.Annotations;
using Lynx.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable PropertyCanBeMadeInitOnly.Global

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

        modelBuilder.Entity<Alone>(b =>
        {
            b.HasKey(x => new { x.Id1, x.Id2 });
            b.Property<bool>("Deleted");
        });

        modelBuilder.Entity<Foreign>();

        modelBuilder.Entity<EntityStrongId>();

        modelBuilder.Entity<EntityStrongIdComposite>()
            .HasKey(x => new { x.Id1, x.Id2 });

        ForeignKeyHelpers.SetForeignKeyCascadeMode(modelBuilder, DeleteBehavior.NoAction);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<StrongId>()
            .HaveConversion<StrongId.EfCoreValueConverter>();
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

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.WithMembers)]
public class EntityBase
{
    public required int Id { get; set; }
}

/// <summary>
/// Parent entity. 1:1 with <see cref="Child"/>
/// </summary>
public class ParentEntity : EntityBase
{
    public required Owned Owned { get; set; }

    public required int? Iteration { get; set; }

    public Child? Child { get; set; }

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
    public required Entity2 Entity2 { get; set; }

    public required bool Deleted { get; set; }

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
                new()
                {
                    Id = id,
                    ForeignId = id,
                    Foreign = new Foreign()
                    {
                        Id = id
                    }
                }
            }
        }
    };
}

public class Entity2 : EntityBase
{
    public required Entity3 Entity3 { get; set; }

    public ICollection<Child>? Children { get; set; }

    public int ParentId { get; set; }
    public required Entity1 Parent { get; set; }
}

public class Entity3 : EntityBase
{
    public required Owned Owned1 { get; set; }
    public required ICollection<Owned> OwnedList { get; set; }

    /// <summary>
    /// Reference self (to test recursion)
    /// </summary>
    public Entity3? Other { get; set; }
}

public class Owned : EntityBase
{
    public Foreign? Child { get; set; }
}

public class Child : EntityBase
{
    public int? ForeignId { get; set; }
    public Foreign? Foreign { get; set; }
}

/// <summary>
/// Entity referenced by an owned type
/// </summary>
public class Foreign : EntityBase;

/// <summary>
/// Not reference by any other entity. Has composite key. Has shadow property Deleted.
/// </summary>
public class Alone
{
    public required int Id1 { get; set; }

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

/// <summary>
/// Entity with a strong id
/// </summary>
public class EntityStrongId
{
    public required StrongId Id { get; set; }

    public static EntityStrongId New(int id) => new() { Id = new StrongId(id) };

    /// <summary>
    /// For testing <see cref="LynxDoNotIncludeReferencedAttribute"/>
    /// </summary>
    [LynxDoNotIncludeReferenced]
    public Foreign? Foreign { get; set; }
}

/// <summary>
/// Entity with a composite strong id
/// </summary>
public class EntityStrongIdComposite
{
    public required StrongId Id1 { get; set; }
    public required StrongId Id2 { get; set; }

    public static EntityStrongIdComposite New(int id) => new()
    {
        Id1 = new StrongId(id),
        Id2 = new StrongId(id * 2)
    };
}

public readonly record struct StrongId(int Value)
{
    public class EfCoreValueConverter : ValueConverter<StrongId, int>
    {
        public EfCoreValueConverter()
            : base(x => x.Value, x => new StrongId(x))
        {
        }
    }
}
