using Microsoft.EntityFrameworkCore;

namespace NodaTime.Absolute.Tests;

public class TestContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity>();
    }
}


public class Entity
{
    public required int Id { get; init; }

    public required AbsoluteDateTime Date { get; init; }

    public required AbsoluteDateTime? DateNull { get; init; }
}