using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Lynx.Providers.Tests;

public class TestContext : DbContext
{
    public TestContext(DbContextOptions options) : base(options) { }

    public DbSet<City> Cities => Set<City>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(b =>
        {
            b.Property(x => x.Population)
                .HasColumnName("Population_Long");

            b.OwnsMany(x => x.Buildings)
                .ToJson("Buildings_Json");

            b.ComplexProperty(x => x.LegalSystem);
        });

        modelBuilder.Entity<Contact>();

        modelBuilder.Entity<Customer>();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<CityId>()
            .HaveConversion<CityId.EfCoreValueConverter>();
    }
}

/*
 * Database structure:
 * Cities - standalone table
 * Customers - table with 2 complex types (of same CLR type)
 *
 * Publisher
 * Author - foreign key to publisher
 * Book - foreign key to Author
 */

public record City
{
    public required CityId Id { get; set; }

    public required string Name { get; set; }

    [Column("Country_Name")]
    public string? Country { get; set; }

    public long? Population { get; set; }

    public required CityLocation Location { get; set; }

    public LegalSystem LegalSystem { get; set; }

    public Building? FamousBuilding { get; set; }

    [Column("Buildings_Json")] // Not used, set above in OnModelCreating
    public List<Building>? Buildings { get; set; }
}

public readonly record struct LegalSystem(bool CommonLaw, bool CivilLaw);

[ComplexType]
public record CityLocation
{
    public decimal Latitude { get; set; }

    public double Longitude { get; set; }

    public float Elevation { get; set; }
}

[Owned]
public record Building
{
    public required string Name { get; set; }

    public BuildingPurpose? Purpose { get; set; }

    public BuildingOwner? Owner { get; set; }
}

[Owned]
public record BuildingOwner
{
    public string? Company { get; set; }

    public DateTime? Since { get; set; }
}

public enum BuildingPurpose
{
    Residential,
    Commercial,
    Governmental
}

public record Customer
{
    public required int Id { get; set; }

    public string? Name { get; set; }

    public string[]? Tags { get; set; }

    public required Address BillingAddress { get; set; }

    public required Address ShippingAddress { get; set; }

    public required CustomerContactInfo OrderContact { get; set; }

    public CustomerContactInfo? InvoiceContact { get; set; }
}

public record Contact
{
    public required int Id { get; set; }

    public string? Name { get; set; }
}

[ComplexType]
public record Address
{
    public required string Street { get; set; }

    public required string City { get; set; }
}

[Owned]
public record CustomerContactInfo
{
    public DateTime? LastContact { get; set; }

    public required int ContactId { get; set; }

    public Contact Contact { get; set; } = null!;
}

public readonly record struct CityId(int Value) : IStrongId
{
    public class EfCoreValueConverter : ValueConverter<CityId, int>
    {
        public EfCoreValueConverter() : base(
            v => v.Value,
            v => new CityId(v)) {}
    }
}

/// <summary>
/// Tagging interface for strong identifiers.
/// </summary>
public interface IStrongId {}