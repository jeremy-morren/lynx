using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lynx.Providers.Tests;

public class TestContext : DbContext
{
    public TestContext(DbContextOptions options) : base(options) { }

    public DbSet<City> Cities => Set<City>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(b =>
        {
            b.Property(x => x.Population)
                .HasColumnName("Population_Long");
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<CityId>()
            .HaveConversion<CityId.EfCoreValueConverter>();
    }
}

public record City
{
    public required CityId Id { get; init; }

    public required string Name { get; init; }

    [Column("Country_Name")]
    public string? Country { get; init; }

    public decimal Latitude { get; init; }

    public double Longitude { get; init; }

    public float Elevation { get; init; }

    public long? Population { get; init; }
}

public readonly record struct CityId(int Value)
{
    public class EfCoreValueConverter : ValueConverter<CityId, int>
    {
        public EfCoreValueConverter() : base(
            v => v.Value,
            v => new CityId(v)) {}
    }
}

/*
 * Database structure:
 * Cities - standalone table
 *
 * Publisher
 * Author - foreign key to publisher
 * Book - foreign key to Author
 */