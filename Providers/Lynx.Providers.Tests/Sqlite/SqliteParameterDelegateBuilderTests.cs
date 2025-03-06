using System.Collections.Immutable;
using System.Data.Common;
using System.Text.Json;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Common.Reflection;
using Lynx.Provider.Sqlite;
using Microsoft.Data.Sqlite;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace Lynx.Providers.Tests.Sqlite;

public class SqliteParameterDelegateBuilderTests : ParameterDelegateBuilderTestsBase
{
    [Theory]
    [InlineData(typeof(City))]
    [InlineData(typeof(Customer))]
    public void BuildAddDelegate(Type entityType)
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(entityType, context.Model);

        var action = AddParameterDelegateBuilder<SqliteCommand, SqliteDbJsonMapper>.Build(entity);

        var command = new SqliteCommand();
        action(command);

        var allProperties = entity.Keys.Concat(entity.GetAllScalarColumns()).ToList();

        allProperties.Should()
            .NotContain(e => e.ColumnIndex < 0)
            .And.BeInAscendingOrder(e => e.ColumnIndex)
            .And.OnlyHaveUniqueItems();

        command.Parameters.Cast<DbParameter>()
            .Select(p => p.ParameterName)
            .Should().BeEquivalentTo(allProperties
                .OrderBy(e => e.ColumnIndex)
                .Select(e => e.ColumnName.SqlParamName));
    }

    [Fact]
    public void BuildCitySetParametersDelegate()
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(typeof(City), context.Model);

        var addParameters = AddParameterDelegateBuilder<SqliteCommand, SqliteDbJsonMapper>.Build(entity);

        var command = new SqliteCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<SqliteCommand, SqliteDbJsonMapper, City>.Build(entity);

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
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Since)], city.FamousBuilding?.Owner?.Since);
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

        var entity = EntityInfoFactory.Create(typeof(Customer), context.Model);

        var addParameters = AddParameterDelegateBuilder<SqliteCommand, SqliteDbJsonMapper>.Build(entity);

        var command = new SqliteCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<SqliteCommand, SqliteDbJsonMapper, Customer>.Build(entity);

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

            command.Parameters.Count.ShouldBe(entity.Keys.Count + entity.GetAllScalarColumns().Count());

            return;

            void Verify(ImmutableArray<string> property, object? value) =>
                VerifyParameter(property, value, entity, command);
        });
    }

    private static string? SerializeJson<T>(T? value) =>
        value == null ? null : JsonSerializer.Serialize(value, JsonSerializerOptions.Default);
}