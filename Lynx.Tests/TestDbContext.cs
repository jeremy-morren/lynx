using Lynx.DocumentStore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

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

    public static IDisposable Create(out Func<TestDbContext> factory) => Create(null, out factory);
    public static IDisposable Create(Mock<IDocumentSessionListener>? listener, out Func<TestDbContext> factory)
    {
        listener ??= new Mock<IDocumentSessionListener>();
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .UseDocumentListener(listener.Object)
            .Options;

        var script = new TestDbContext(options).Database.GenerateCreateScript();
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
    public required int Id { get; set; }
        
    public required OwnedType OwnedType { get; set; }
    
    public required int? Iteration { get; set; }

    public static TestEntity Create(int id, int? iteration = null) => new()
    {
        Id = id,
        OwnedType = new OwnedType() { Id = id },
        Iteration = iteration
    };
}

public record OwnedType
{
    public required int Id { get; set; }
}