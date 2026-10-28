using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using NodaTime.Text;
// ReSharper disable UnusedMember.Global

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

#if NET10_0_OR_GREATER
    public DbSet<Order> Orders => Set<Order>();
#endif

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

        modelBuilder.Entity<Contact>(b =>
        {
            b.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Customer>(c =>
        {
            c.Property(x => x.Id).ValueGeneratedNever();

            c.OwnsOne(x => x.Cat, Cat.Configure);
            c.OwnsMany(x => x.Cats, Cat.Configure);
            c.OwnsOne(x => x.OrderContact)
                .HasOne(x => x.Contact)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction);
        });

#if NET10_0_OR_GREATER
        modelBuilder.Entity<Order>(Order.Configure);
#endif

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
        if (Database.ProviderName?.Contains("Npgsql") != true)
        {
            configurationBuilder.Properties<LocalDateTime>()
                .HaveConversion<LocalDateTimeConverter>();

            configurationBuilder.Properties<Instant>()
                .HaveConversion<InstantConverter>();
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

    public LocalDateTime? Since { get; set; }
}

public enum BuildingPurpose
{
    Residential,
    Commercial,
    Governmental
}

public record Contact
{
    public required int Id { get; set; }

    public string? Name { get; set; }
}

/// <summary>
/// Customer, for testing owned and complex types.
/// </summary>
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
    /// Owns one, mapped to JSON
    /// </summary>
    public required Cat? Cat { get; set; }

    /// <summary>
    /// Owns many, mapped to JSON
    /// </summary>
    public required List<Cat>? Cats { get; set; }
}

[ComplexType]
public record Address
{
    public required string Street { get; set; }

    public required string City { get; set; }

    public LocalDateTime LastUpdated { get; set; } = MillisecondClock.LocalNow;
}

[Owned]
public record CustomerContactInfo
{
    public DateTime? LastContact { get; set; }
    public DateTimeOffset? LastContactUtc { get; set; }
    public DateOnly? LastContactDate { get; set; }

    public required int ContactId { get; set; }

    public Contact Contact { get; set; } = null!;
}

/// <summary>
/// Owned type, testing JSON serialization.
/// </summary>
[Owned]
public record Cat
{
    public required string Name { get; init; }

    public Instant BirthDate { get; init; } = MillisecondClock.Now;

    /// <summary>
    /// Owns one, mapped to JSON
    /// </summary>
    [JsonPropertyName("FirstKitten_Json")]
    public Kitten? FirstKitten { get; set; } = new()
    {
        Name = "Kitten 1",
        DeathDate = MillisecondClock.LocalNow
    };

    /// <summary>
    /// Owns many, mapped to JSON
    /// </summary>
    public List<Kitten>? Kittens { get; set; } =
    [
        new Kitten() { Name = "Kitten 2" },
        new Kitten() { Name = "Kitten 3" }
    ];

    // Apparently EF Core doesn't support complex types in owned types mapped to JSON

    // public Address Address { get; set; } = new()
    // {
    //     Street = "Street 1",
    //     City = "City 1"
    // };

    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, Cat> builder)
        where TOwner : class
    {
        builder.ToJson();
        builder.OwnsOne(x => x.FirstKitten).HasJsonPropertyName("FirstKitten_Json");
        builder.OwnsMany(x => x.Kittens);
    }
}

/// <summary>
/// Owned type. Testing nested JSON serialization.
/// </summary>
[Owned]
public record Kitten
{
    public required string Name { get; init; }

    public LocalDateTime DeathDate { get; init; } = MillisecondClock.LocalNow;
}

#if NET10_0_OR_GREATER

/// <summary>
/// Order, for testing complex types (EF Core 10+): optional complex types, complex types mapped to JSON and complex collections.
/// </summary>
public record Order
{
    public required int Id { get; set; }

    /// <summary>
    /// Required complex type (table splitting)
    /// </summary>
    public required Address BillingAddress { get; set; }

    /// <summary>
    /// Optional complex type (table splitting)
    /// </summary>
    public Address? ShippingAddress { get; set; }

    /// <summary>
    /// Optional struct complex type (table splitting)
    /// </summary>
    public LegalSystem? LegalSystem { get; set; }

    /// <summary>
    /// Required complex type mapped to JSON
    /// </summary>
    public required OrderDetails Details { get; set; }

    /// <summary>
    /// Optional complex type mapped to JSON (with a custom column name)
    /// </summary>
    public OrderDetails? ExtraDetails { get; set; }

    /// <summary>
    /// Complex collection mapped to JSON
    /// </summary>
    public required List<OrderLine> Lines { get; set; }

    public static void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.ComplexProperty(x => x.BillingAddress);
        builder.ComplexProperty(x => x.ShippingAddress);
        builder.ComplexProperty(x => x.LegalSystem);

        builder.ComplexProperty(x => x.Details, b =>
        {
            b.ToJson();
            b.Property(x => x.Notes).HasJsonPropertyName("Notes_Json");
        });
        builder.ComplexProperty(x => x.ExtraDetails, b => b.ToJson("Extra_Details_Json"));
        builder.ComplexCollection(x => x.Lines, b => b.ToJson());
    }
}

public record OrderDetails
{
    public required string Notes { get; set; }

    /// <summary>
    /// Scalar with a value converter inside JSON
    /// </summary>
    public CityId? City { get; set; }

    public Instant? PlacedAt { get; set; }

    public required string[] Tags { get; set; }

    /// <summary>
    /// Nested optional complex type inside JSON
    /// </summary>
    public Address? Delivery { get; set; }

    /// <summary>
    /// Nested complex collection inside JSON
    /// </summary>
    public required List<OrderLine> Lines { get; set; }
}

public record OrderLine
{
    public required string Product { get; set; }

    public decimal Quantity { get; set; }

    public LocalDateTime? Due { get; set; }

    public Address? Destination { get; set; }
}

#endif

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

public readonly record struct CityId(string Value) : IStrongId
{
    public CityId(int value) : this(value.ToString()) {}

    public class EfCoreValueConverter : ValueConverter<CityId, string>
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

#region Converters

public class LocalDateTimeConverter : ValueConverter<LocalDateTime, string>
{
    public LocalDateTimeConverter() : base(
        v => LocalDateTimePattern.GeneralIso.Format(v),
        v => LocalDateTimePattern.GeneralIso.Parse(v).Value) 
    {
    }
}

public class InstantConverter : ValueConverter<Instant, string>
{
    public InstantConverter() : base(
        v => InstantPattern.General.Format(v),
        v => InstantPattern.General.Parse(v).Value)
    {
    }
}

#endregion