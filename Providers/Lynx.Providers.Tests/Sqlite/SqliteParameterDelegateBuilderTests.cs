using System.Collections.Immutable;
using System.Text.Json;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Reflection;
using Lynx.Provider.Sqlite;
using Microsoft.Data.Sqlite;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace Lynx.Providers.Tests.Sqlite;

public class SqliteParameterDelegateBuilderTests : ParameterDelegateBuilderTestsBase
{
    [Theory]
    [MemberData(nameof(GetEntityTypes))]
    public void BuildAddDelegate(Type entityType)
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactoryTests.CreateRootEntity(entityType, context.Model);
        var action = AddParameterDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder>.Build(entity);
        var command = new SqliteCommand();
        action(command);

        VerifyEntityInfo(entity, command);
    }

    [Fact]
    public void BuildCitySetParametersDelegate()
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.CreateRoot<City>(context.Model);

        var addParameters = AddParameterDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder>.Build(entity);

        var command = new SqliteCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder, City>.Build(entity);

        Assert.All(GetTestCities(), city =>
        {
            setParameters(command, city);

            Verify([nameof(City.Id)], city.Id.Value);
            Verify([nameof(City.Name)], city.Name);
            Verify([nameof(City.Country)], city.Country);
            Verify([nameof(City.Population)], city.Population);
            Verify([nameof(City.Location), nameof(CityLocation.Latitude)], city.Location?.Latitude);
            Verify([nameof(City.Location), nameof(CityLocation.Longitude)], city.Location?.Longitude);
            Verify([nameof(City.Location), nameof(CityLocation.Elevation)], city.Location?.Elevation);
            Verify([nameof(City.Location), nameof(CityLocation.StreetWidths)], SerializeJson(city.Location?.StreetWidths));
            Verify([nameof(City.LegalSystem), nameof(LegalSystem.CommonLaw)], city.LegalSystem.CommonLaw);
            Verify([nameof(City.LegalSystem), nameof(LegalSystem.CivilLaw)], city.LegalSystem.CivilLaw);
            Verify([nameof(City.FamousBuilding), nameof(Building.Name)], city.FamousBuilding?.Name);
            Verify([nameof(City.FamousBuilding), nameof(Building.Purpose)], city.FamousBuilding?.Purpose);
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Company)], city.FamousBuilding?.Owner?.Company);
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Since)], 
                city.FamousBuilding?.Owner?.Since?.ToString("yyyy-MM-ddTHH:mm:ss", null));
            Verify([nameof(City.Buildings)], SerializeJson(city.Buildings));

            command.Parameters.Count.ShouldBe(entity.GetAllScalarColumns().Count() + entity.Keys.Count);

            return;

            void Verify(ImmutableArray<string> property, object? value) =>
                VerifyParameter(property, value, entity, command);
        });
    }

    [Fact]
    public void BuildCustomerSetParametersDelegate()
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.CreateRoot<Customer>(context.Model);

        var addParameters = AddParameterDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder>.Build(entity);

        var command = new SqliteCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder, Customer>.Build(entity);

        Assert.All(GetTestCustomers(), customer =>
        {
            setParameters(command, customer);

            Verify([nameof(Customer.Id)], customer.Id);
            Verify([nameof(Customer.Name)], customer.Name);
            Verify([nameof(Customer.Tags)], SerializeJson(customer.Tags));
            Verify([nameof(Customer.BillingAddress), nameof(Address.City)], customer.BillingAddress?.City);
            Verify([nameof(Customer.BillingAddress), nameof(Address.Street)], customer.BillingAddress?.Street);
            Verify([nameof(Customer.ShippingAddress), nameof(Address.City)], customer.ShippingAddress?.City);
            Verify([nameof(Customer.ShippingAddress), nameof(Address.Street)], customer.ShippingAddress?.Street);
            Verify([nameof(Customer.OrderContact), nameof(CustomerContactInfo.ContactId)], customer.OrderContact?.ContactId);
            Verify([nameof(Customer.OrderContact), nameof(CustomerContactInfo.LastContact)], customer.OrderContact?.LastContact);
            Verify([nameof(Customer.InvoiceContact), nameof(CustomerContactInfo.ContactId)], customer.InvoiceContact?.ContactId);
            Verify([nameof(Customer.InvoiceContact), nameof(CustomerContactInfo.LastContact)], customer.InvoiceContact?.LastContact);
            Verify([nameof(Customer.Cat)], SerializeJson(customer.Cat));
            Verify([nameof(Customer.Cats)], SerializeJson(customer.Cats));

            command.Parameters.Count.ShouldBe(entity.Keys.Count + entity.GetAllScalarColumns().Count());

            return;

            void Verify(ImmutableArray<string> property, object? value) =>
                VerifyParameter(property, value, entity, command);
        });
    }

    [Fact]
    public void BuildConverterEntitySetParametersDelegate()
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.CreateRoot<ConverterEntity>(context.Model);

        var addParameters = AddParameterDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder>.Build(entity);

        var command = new SqliteCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder, ConverterEntity>.Build(entity);

        Assert.All(GetTestConverterEntities(), value =>
        {
            setParameters(command, value);
            
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
            
            command.Parameters.Count.ShouldBe(entity.Keys.Count + entity.GetAllScalarColumns().Count());

            return;

            void Verify(ImmutableArray<string> property, object? v) =>
                VerifyParameter(property, v, entity, command);
        });
    }

    public static string? SerializeJson(object? value) => JsonHelpers.SerializeJson(value);
}