using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Lynx.Providers.Common.Entities;
using Lynx.Provider.Npgsql;
using Moq;
using Npgsql;
using NpgsqlTypes;

// ReSharper disable UseRawString
// ReSharper disable MethodSupportsCancellation
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace Lynx.Providers.Tests.Npgsql;

[SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public class NpgsqlEntityColumnBuilderTests : ParameterDelegateBuilderTestsBase
{
    [Theory]
    [MemberData(nameof(GetEntityTypes))]
    public void CommandBuilderTests(Type entityType)
    {
        using var harness = new NpgsqlTestHarness([nameof(CommandBuilderTests), entityType]);
        using var context = harness.CreateContext();

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var method = typeof(NpgsqlEntityColumnBuilderTests)
            .GetMethod(nameof(CommandBuilderTestsGeneric), flags)
            .ShouldNotBeNull();
        method.MakeGenericMethod(entityType).Invoke(null, [context]);
    }

    private static void CommandBuilderTestsGeneric<T>(TestContext context) where T : class
    {
        var entity = EntityInfoFactory.CreateRoot<T>(context.Model);
        var expected = entity.Keys.Concat(entity.GetAllScalarColumns()).ToList();

        var columns = NpgsqlEntityColumnBuilder<T, NpgsqlBinaryImporter>.GetColumnInfo(entity);
        columns.Select(c => c.Property.Name)
            .Should().BeEquivalentTo(expected.Select(c => c.Name));

        var generator = new NpgsqlCommandGenerator(entity);
        var commands = new[]
        {
            generator.GenerateBinaryCopyInsertCommand(),
            generator.GenerateBinaryCopyTempTableInsertCommand()
        };
        Assert.All(commands, command =>
            Regex.Matches(command, @"(?<=[,\(]\W*?"")\w+(?=""[,\)])")
                .Select(m => m.Value)
                .Should()
                .BeEquivalentTo(columns.Select(c => c.Property.ColumnName.SqlColumnName),
                    "Columns in the command should match the entity columns"));

        generator.GetCreateTempTableCommand().ShouldContain(generator.TempTableName);
        generator.GenerateUpsertTempTableCommand().Should().Contain(generator.TempTableName)
            .And.Contain(generator.QualifiedTableName);
    }

    [Fact]
    public void BuildCitySetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildCitySetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.CreateRoot<City>(context.Model);
        var columns = NpgsqlEntityColumnBuilder<City, ITestWriter>.GetColumnInfo(entity);
        columns.Should().HaveCount(entity.GetAllScalarColumns().Count() + entity.Keys.Count);

        Assert.All(GetTestCities(), city =>
        {
            Verify([nameof(City.Id)], city.Id.Value);
            Verify([nameof(City.Name)], city.Name);
            Verify([nameof(City.Country)], city.Country);
            Verify([nameof(City.Population)], city.Population);
            Verify([nameof(City.Location), nameof(CityLocation.Latitude)], city.Location?.Latitude);
            Verify([nameof(City.Location), nameof(CityLocation.Longitude)], city.Location?.Longitude);
            Verify([nameof(City.Location), nameof(CityLocation.Elevation)], city.Location?.Elevation);
            Verify([nameof(City.Location), nameof(CityLocation.StreetWidths)], city.Location?.StreetWidths);
            Verify([nameof(City.LegalSystem), nameof(LegalSystem.CommonLaw)], city.LegalSystem.CommonLaw);
            Verify([nameof(City.LegalSystem), nameof(LegalSystem.CivilLaw)], city.LegalSystem.CivilLaw);
            Verify([nameof(City.FamousBuilding), nameof(Building.Name)], city.FamousBuilding?.Name);
            Verify([nameof(City.FamousBuilding), nameof(Building.Purpose)], (int?)city.FamousBuilding?.Purpose);
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Company)], city.FamousBuilding?.Owner?.Company);
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Since)], city.FamousBuilding?.Owner?.Since);
            Verify([nameof(City.Buildings)], SerializeJson(city.Buildings));

            return;

            void Verify<TValue>(ImmutableArray<string> property, TValue? value) =>
                VerifyColumn(property, value, city, columns);
        });
    }
    
    [Fact]
    public void BuildCustomerSetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildCustomerSetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.CreateRoot<Customer>(context.Model);

        var columns = NpgsqlEntityColumnBuilder<Customer, ITestWriter>.GetColumnInfo(entity);
        columns.Should().HaveCount(entity.GetAllScalarColumns().Count() + entity.Keys.Count);

        Assert.All(GetTestCustomers(), customer =>
        {
            Verify([nameof(Customer.Id)], customer.Id);
            Verify([nameof(Customer.Name)], customer.Name);
            Verify([nameof(Customer.Tags)], customer.Tags);
            Verify([nameof(Customer.BillingAddress), nameof(Address.City)], customer.BillingAddress?.City);
            Verify([nameof(Customer.BillingAddress), nameof(Address.Street)], customer.BillingAddress?.Street);
            Verify([nameof(Customer.ShippingAddress), nameof(Address.City)], customer.ShippingAddress?.City);
            Verify([nameof(Customer.ShippingAddress), nameof(Address.Street)], customer.ShippingAddress?.Street);
            Verify([nameof(Customer.OrderContact), nameof(CustomerContactInfo.ContactId)], customer.OrderContact?.ContactId);
            Verify([nameof(Customer.OrderContact), nameof(CustomerContactInfo.LastContact)], customer.OrderContact?.LastContact);
            Verify([nameof(Customer.InvoiceContact), nameof(CustomerContactInfo.ContactId)], customer.InvoiceContact?.ContactId);
            Verify([nameof(Customer.InvoiceContact), nameof(CustomerContactInfo.LastContact)], customer.InvoiceContact?.LastContact);

            return;

            void Verify<TValue>(ImmutableArray<string> property, TValue? value) =>
                VerifyColumn(property, value, customer, columns);
        });
    }

    [Fact]
    public void BuildConverterEntitySetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildConverterEntitySetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.CreateRoot<ConverterEntity>(context.Model);

        var columns = NpgsqlEntityColumnBuilder<ConverterEntity, ITestWriter>.GetColumnInfo(entity);
        columns.Should().HaveCount(entity.GetAllScalarColumns().Count() + entity.Keys.Count);

        Assert.All(GetTestConverterEntities(), value =>
        {
            Verify([nameof(ConverterEntity.Id1)], value.Id1.Value);
            Verify([nameof(ConverterEntity.Id2)], value.Id2.Value);
            Verify([nameof(ConverterEntity.NullableId)], value.NullableId?.Value);
            Verify([nameof(ConverterEntity.NullableValueId)], value.NullableValueId?.Value);
            Verify([nameof(ConverterEntity.ReferenceId)], value.ReferenceId?.Value);
            Verify([nameof(ConverterEntity.ReferenceNullableIntId)], value.ReferenceNullableIntId?.Value);
            Verify([nameof(ConverterEntity.ReferenceIntId)], value.ReferenceIntId?.Value);
            Verify([nameof(ConverterEntity.IntValue)], value.IntValue);
            Verify([nameof(ConverterEntity.StringValue)], value.StringValue);
            Verify([nameof(ConverterEntity.IntValueNull)], value.IntValueNull);
            Verify([nameof(ConverterEntity.Enum)], value.Enum?.ToString());
            
            return;

            void Verify<TValue>(ImmutableArray<string> property, TValue? v) =>
                VerifyColumn(property, v, value, columns);
        });
    }

    private static void VerifyColumn<TEntity, TValue>(
        ImmutableArray<string> property,
        TValue? value,
        TEntity entity,
        NpgsqlEntityColumn<TEntity, ITestWriter>[] columns)
    {
        var column = columns.Where(c => c.Property.Name == property).ShouldHaveSingleItem();
        var dbType = column.DBType.ShouldNotBeNull(); // TODO: Test with unknown DBType

        var cancellationToken = new CancellationTokenSource().Token;

        if (value != null)
        {
            var mock = new Mock<ITestWriter>(MockBehavior.Strict);
            mock.Setup(w => w.Write(value, dbType))
                .Verifiable($"Write should be called for column {column.Property.Name}");
            mock.Setup(w => w.WriteAsync(value, dbType, cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable($"WriteAsync should be called for column {column.Property.Name}");

            column.Write(entity, mock.Object);
            column.WriteAsync(entity, mock.Object, cancellationToken).Wait();

            mock.Verify();
        }
        else
        {
            var mock = new Mock<ITestWriter>(MockBehavior.Strict);
            mock.Setup(w => w.WriteNull())
                .Verifiable($"WriteNull should be called for column {column.Property.Name}");
            mock.Setup(w => w.WriteNullAsync(cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable($"WriteNullAsync should be called for column {column.Property.Name}");
            column.Write(entity, mock.Object);
            column.WriteAsync(entity, mock.Object, cancellationToken).ShouldBe(Task.CompletedTask);
            mock.Verify();
        }
    }

    /// <summary>
    /// Test interface to mirror <see cref="NpgsqlBinaryImporter"/>
    /// </summary>
    public interface ITestWriter
    {
        void Write<T>(T value);

        void Write<T>(T value, NpgsqlDbType type);

        void WriteNull();

        Task WriteAsync<T>(T value, CancellationToken ct);

        Task WriteAsync<T>(T value, NpgsqlDbType type, CancellationToken ct);

        Task WriteNullAsync(CancellationToken ct);
    }

    public static string? SerializeJson(object? value) => JsonHelpers.SerializeJson(value);
}