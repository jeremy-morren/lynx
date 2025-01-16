using Lynx.DocumentStore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Lynx.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options)
        : base(options) {}

    public IQueryable<T> Query<T>() where T : class => Set<T>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.OwnsOne(x => x.OwnedType);
        });
    }

    /// <summary>
    /// Creates a test context with an in-memory sqlite database
    /// </summary>
    /// <param name="factory"></param>
    /// <returns></returns>
    public static IDisposable CreateContext(out Func<TestDbContext> factory)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        factory = () =>
        {
            var context = new TestDbContext(options);
            context.Database.EnsureCreated();
            return context;
        };

        return connection;
    }
}

public record TestEntity
{
    public required int Id { get; init; }
        
    public required OwnedType OwnedType { get; init; }
    
    public required int? Iteration { get; init; }

    public ChildEntity? Child { get; init; }

    public static TestEntity Create(int id, int? iteration = null, int? childId = null) => new()
    {
        Id = id,
        Iteration = iteration,
        OwnedType = new OwnedType() { Id = id },
        Child = childId.HasValue ? new ChildEntity() { Id = childId.Value } : null
    };
}

public record OwnedType
{
    public required int Id { get; init; }
}

public record ChildEntity
{
    public required int Id { get; init; }
}