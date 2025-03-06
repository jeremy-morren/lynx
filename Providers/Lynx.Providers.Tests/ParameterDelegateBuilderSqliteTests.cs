using System.Collections.Immutable;
using System.Data.Common;
using System.Text.Json;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Common.Models;
using Lynx.Provider.Common.Reflection;
using Lynx.Provider.Sqlite;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.Data.Sqlite;
using NodaTime;

namespace Lynx.Providers.Tests;

public class ParameterDelegateBuilderTestsBase
{
    internal static void VerifyParameter(ImmutableArray<string> property, object? value, RootEntityInfo entity, DbCommand command)
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

    protected static City[] GetTestCities() =>
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
                Elevation = 1,
                StreetWidths = [5],
            },
            FamousBuilding = new Building()
            {
                Name = "Parliament",
                Purpose = BuildingPurpose.Governmental,
                Owner = new BuildingOwner()
                {
                    Company = "Ralph Gonsalves",
                    Since = new LocalDate(2001, 3, 28)
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
                Elevation = -5,
                StreetWidths = null!
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
                Elevation = 20,
                StreetWidths = []
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

    protected static Customer[] GetTestCustomers() =>
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