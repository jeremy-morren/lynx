using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Text;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable PropertyCanBeMadeInitOnly.Global
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

    public DbSet<ConverterEntity> ConverterEntities => Set<ConverterEntity>();
    
    public DbSet<IdOnly> IdOnly => Set<IdOnly>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdOnly>();
        
        modelBuilder.Entity<City>(b =>
        {
            b.Property(x => x.Population)
                .HasColumnName("Population_Long");

            b.OwnsMany(x => x.Buildings)
                .ToJson("Buildings_Json");

            b.ComplexProperty(x => x.LegalSystem);
        });

        modelBuilder.Entity<Contact>();

        modelBuilder.Entity<Customer>(c =>
        {
            c.OwnsMany(x => x.Cats).ToJson();
        });

        modelBuilder.Entity<ConverterEntity>(b =>
        {
            // composite key
            b.HasKey(c => new { c.Id1, c.Id2 });

            b.Property(x => x.Enum)
                .HasConversion(p => p!.ToString(), p => Enum.Parse<BuildingPurpose>(p!));

            b.Property(x => x.IntValue)
                .HasConversion<ConverterHandleNulls<int>>();
            
            b.Property(x => x.IntValueNull)
                .HasConversion<ConverterHandleNulls<int?>>();
            b.Property(x => x.StringValue)
                .HasConversion<ConverterHandleNulls<string>>();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<CityId>()
            .HaveConversion<CityId.EfCoreValueConverter>();
        configurationBuilder.Properties<StringId>()
            .HaveConversion<StringId.EfCoreValueConverter>();
        configurationBuilder.Properties<ReferenceStringId>()
            .HaveConversion<ReferenceStringId.EfCoreValueConverter>();
        configurationBuilder.Properties<ReferenceIntId>()
            .HaveConversion<ReferenceIntId.EfCoreValueConverter>();
        configurationBuilder.Properties<ReferenceNullableIntId>()
            .HaveConversion<ReferenceNullableIntId.EfCoreValueConverter>();
        
        // Test provider-specific converter
        if (Database.ProviderName?.Contains("Sqlite") == true)
        {
            configurationBuilder.Properties<LocalDate>()
                .HaveConversion<LocalDateConverter>();
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
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
    
    public required short[] StreetWidths { get; set; }
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

    public LocalDate? Since { get; set; }
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

    /// <summary>
    /// Owns many, mapped to JSON
    /// </summary>
    public required List<Cat>? Cats { get; set; }
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

[Owned]
public record Cat
{
    public required string Name { get; init; }
}

/// <summary>
/// Entity with only an Id.
/// </summary>
public record IdOnly
{
    public long Id { get; set; }
}

/// <summary>
/// Tagging interface for strong identifiers.
/// </summary>
public interface IStrongId {}

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
/// Entity with various types of converters.
/// </summary>
public class ConverterEntity
{
    // Not null value type -> reference type
    public StringId Id1 { get; set; }

    // value type -> value type
    public CityId Id2 { get; set; }

    // Null value type -> reference type
    public StringId? NullableId { get; set; }

    // value type -> nullable value type
    public CityId? NullableValueId { get; set; }

    // reference type -> reference type
    public ReferenceStringId? ReferenceId { get; set; }

    // reference type -> value type
    public ReferenceIntId? ReferenceIntId { get; set; }

    // reference type -> nullable value type
    public ReferenceNullableIntId? ReferenceNullableIntId { get; set; }

    // value type -> reference type
    // inline converter defined in OnModelCreating
    public BuildingPurpose? Enum { get; set; }

    // For testing converters that handles nulls
    
    public int IntValue { get; set; }
    
    public int? IntValueNull { get; set; }
    
    public string? StringValue { get; set; }
}

public readonly record struct StringId(string Value) : IStrongId
{
    public class EfCoreValueConverter : ValueConverter<StringId, string>
    {
        public EfCoreValueConverter() : base(
            v => v.Value,
            v => new StringId(v)) {}
    }
}

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class ConverterHandleNulls<T> : ValueConverter<T, T>
{
    public ConverterHandleNulls() : base(
        v => v,
        v => v,
        true) {}
}

public record ReferenceIntId(int Value) : IStrongId
{
    public class EfCoreValueConverter : ValueConverter<ReferenceIntId, int>
    {
        public EfCoreValueConverter() : base(
            v => v.Value,
            v => new ReferenceIntId(v)) {}
    }
}

// NB: we don't handle nulls, because we're testing converter processing
// These converters don't make sense in real life,
// because EF core will handle nulls by setting the property to null i.e. converter isn't called

public record ReferenceStringId(string? Value) : IStrongId
{
    public class EfCoreValueConverter : ValueConverter<ReferenceStringId, string?>
    {
        public EfCoreValueConverter() : base(
            v => v.Value,
            v => new ReferenceStringId(v)) {}
    }
}

public record ReferenceNullableIntId(int? Value) : IStrongId
{
    public class EfCoreValueConverter : ValueConverter<ReferenceNullableIntId, int?>
    {
        public EfCoreValueConverter() : base(
            v => v.Value,
            v => new ReferenceNullableIntId(v)) {}
    }
}

    
public class LocalDateConverter : ValueConverter<LocalDate, string>
{
    public LocalDateConverter() : base(
        v => LocalDatePattern.Iso.Format(v),
        v => LocalDatePattern.Iso.Parse(v).Value) 
    {
    }
}