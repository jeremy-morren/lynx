using System.Collections.Immutable;
using System.Data.Common;
using System.Text.Json;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Common.Models;
using Lynx.Provider.Common.Reflection;
using Lynx.Provider.Sqlite;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.Data.Sqlite;

namespace Lynx.Providers.Tests;

public class ParameterDelegateBuilderTests
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

    private static void VerifyParameter(ImmutableArray<string> property, object? value, RootEntityInfo entity, DbCommand command)
    {
        var name = string.Join(".", property);

        var column = entity.Keys.Concat(entity.GetAllScalarColumns())
            .Where(e => e.Name == property)
            .ShouldHaveSingleItem($"Property {name} not found");

        var parameter = command.Parameters[column.ColumnIndex];
        parameter.ParameterName.ShouldBe(column.ColumnName.SqlParamName);
        if (value == null)
            parameter.Value.ShouldBe(DBNull.Value, $"{name} should be DBNull.Value");
        else
            parameter.Value.ShouldBe(value, $"Invalid value for {name}");
    }

    private static string? SerializeJson<T>(T? value) =>
        value == null ? null : JsonSerializer.Serialize(value, JsonSerializerOptions.Default);

    private static City[] GetTestCities() =>
    [
        new City()
        {
            Id = new CityId(1),
            Name = "Kingstown",
            Country = "St. Vincent",
            LegalSystem = new LegalSystem(true, false),
            Location = new CityLocation()
            {
                Latitude = 50,
                Longitude = 13,
                Elevation = 1
            },
            FamousBuilding = new Building()
            {
                Name = "Parliament",
                Purpose = BuildingPurpose.Governmental,
                Owner = new BuildingOwner()
                {
                    Company = "Ralph Gonsalves",
                    Since = new DateTime(2001, 3, 28)
                }
            }
        },
        new City()
        {
            Id = new CityId(2),
            Name = "Bridgetown",
            Location = new CityLocation()
            {
                Latitude = 48,
                Longitude = 13,
                Elevation = -5
            },
            Buildings =
            [
                new Building()
                {
                    Name = "House"
                }
            ]
        },
        new City()
        {
            Id = new CityId(3),
            Name = "Ashton",
            Population = 200,
            Location = new CityLocation()
            {
                Latitude = 49.5m,
                Longitude = 12.8,
                Elevation = 20
            },
            FamousBuilding = new Building()
            {
                Name = null!,
                Purpose = (BuildingPurpose)75,
                Owner = new BuildingOwner()
            }
        },
        new City()
        {
            Id = new CityId(4),
            Name = "Clifton",
            Population = 500,
            Location = null!
        }
    ];

    private static Customer[] GetTestCustomers() =>
    [
        new Customer()
        {
            Id = 1,
            Name = "Ras John",
            Tags = ["Plants"],

            BillingAddress = new  Address()
            {
                City = "Cumberland",
                Street = null!
            },
            ShippingAddress = null!,

            OrderContact = new CustomerContactInfo()
            {
                ContactId = 5,
                LastContact = DateTime.Now,

                // Should be ignored
                Contact = new Contact()
                {
                    Id = -1
                }
            },

            InvoiceContact = new CustomerContactInfo()
            {
                ContactId = 9
            }
        },

        new Customer
        {
            Id = 2,
            Name = "Guy Walker",
            Tags = null!,

            BillingAddress = null!,
            ShippingAddress = new Address()
            {
                City = string.Empty,
                Street = "Punnet Avenue"
            },

            InvoiceContact = new CustomerContactInfo()
            {
                ContactId = 7,
                LastContact = DateTime.UnixEpoch
            },
            OrderContact = null!
        }
    ];
}