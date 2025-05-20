using Lynx.Providers.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using NpgsqlTypes;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Lynx.Providers.Tests.Npgsql;

public class FullTextSearchTests
{
    [Fact]
    public void DoInsert()
    {
        const string connString = $"{NpgsqlTestHarness.ConnString};Database={nameof(FullTextSearchTests)}";
        var options = new DbContextOptionsBuilder<FullTextSearchContext>()
            .UseNpgsql(connString)
            .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;
        
        using var context = new FullTextSearchContext(options);
        
        var entity = EntityInfoFactory.CreateRoot<Product>(context.Model);
        entity.GetAllScalarColumns()
            .ShouldNotContain(c => ((IProperty)c.Property).ValueGenerated != ValueGenerated.Never);
    }
    
    public class FullTextSearchContext : DbContext
    {
        public FullTextSearchContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasGeneratedTsVectorColumn(
                    p => p.SearchVector,
                    "english", // Text search config
                    p => new { p.Name, p.Description }) // Included properties
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN"); // Index method on the search vector (GIN or GIST)
        }
    }

    public class Product
    {
        public int Id { get; set; }

        public required string Name { get; set; }
        public string? Description { get; set; }
        public NpgsqlTsVector SearchVector { get; set; } = null!;
    }
}