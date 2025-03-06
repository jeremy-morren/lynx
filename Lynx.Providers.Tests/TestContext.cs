using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
// ReSharper disable NotAccessedPositionalProperty.Global

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
    public required CityId Id { get; init; }

    public required string Name { get; init; }

    [Column("Country_Name")]
    public string? Country { get; init; }

    public long? Population { get; init; }

    public required CityLocation Location { get; init; }

    public LegalSystem LegalSystem { get; init; }

    public Building? FamousBuilding { get; init; }

    [Column("Buildings_Json")] // Not used, set above in OnModelCreating
    public Building[]? Buildings { get; init; }
}

public readonly record struct LegalSystem(bool CommonLaw, bool CivilLaw);

[ComplexType]
public record CityLocation
{
    public decimal Latitude { get; init; }

    public double Longitude { get; init; }

    public float Elevation { get; init; }
}

[Owned]
public record Building
{
    public required string Name { get; init; }

    public BuildingPurpose? Purpose { get; init; }
}

public enum BuildingPurpose
{
    Residential,
    Commercial,
    Industrial,
    Religious,
    Educational,
    Governmental,
    Recreational,
    Healthcare,
    Transportation,
    Military,
    Agricultural,
    Other
}

public record Customer
{
    public required int Id { get; init; }

    public string? Name { get; init; }

    public required CustomerContactInfo OrderContact { get; init; }

    public CustomerContactInfo? InvoiceContact { get; init; }

    public required Address BillingAddress { get; init; }

    public required Address ShippingAddress { get; init; }

    public static Customer New(int id) => new()
    {
        Id = id,
        Name = $"Customer {id}",
        OrderContact = new CustomerContactInfo()
        {
            ContactId = id,
            Contact = new Contact()
            {
                Id = id,
            }
        },
        BillingAddress = new Address()
        {
            Street = $"Billing street {id}",
            City = $"Billing city {id}"
        },
        ShippingAddress = new Address()
        {
            Street = $"Shipping street {id}",
            City = $"Shipping city {id}"
        }
    };
}

public record Contact
{
    public required int Id { get; init; }

    public string? Name { get; init; }
}

[ComplexType]
public record Address
{
    public required string Street { get; init; }

    public required string City { get; init; }
}

[Owned]
public record CustomerContactInfo
{
    public DateTime? LastContact { get; init; }

    public required int ContactId { get; init; }

    public Contact Contact { get; init; } = null!;
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