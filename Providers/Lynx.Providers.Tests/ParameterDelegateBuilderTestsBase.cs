using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Models;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Lynx.Providers.Tests;

public class ParameterDelegateBuilderTestsBase
{
    /// <summary>
    /// Gets the entity types to test
    /// </summary>
    public static TheoryData<Type> GetEntityTypes() => new()
    {
        typeof(City),
        typeof(Customer),
        typeof(ConverterEntity)
    };

    internal static void VerifyEntityInfo(RootEntityInfo entity, DbCommand command)
    {
        var allProperties = entity.Keys.Concat(entity.GetAllScalarColumns()).ToList();

        allProperties.Should()
            .NotContain(e => e.ColumnIndex < 0)
            .And.BeInAscendingOrder(e => e.ColumnIndex)
            .And.OnlyHaveUniqueItems(e => e.ColumnIndex);

        command.Parameters.Cast<DbParameter>()
            .Select(p => p.ParameterName)
            .Should().BeEquivalentTo(allProperties
                .OrderBy(e => e.ColumnIndex)
                .Select(e => e.ColumnName.SqlParamName));
    }

    /// <summary>
    /// Verifies that a parameter is correctly set on a command
    /// </summary>
    internal static void VerifyParameter(ImmutableArray<string> property, object? value, RootEntityInfo entity, DbCommand command)
    {
        var name = string.Join(".", property);

        var column = entity.Keys.Concat(entity.GetAllScalarColumns())
            .Where(e => e.Name == property)
            .ShouldHaveSingleItem($"Property {name} not found");

        var parameter = command.Parameters[column.ColumnIndex];
        parameter.ParameterName.ShouldBe(column.ColumnName.SqlParamName);
        if (value == null)
        {
            parameter.Value.ShouldBe(DBNull.Value, $"{name} should be DBNull.Value");
        }
        else if (IsJson(value, out var json))
        {
            var paramValue = parameter.Value.ShouldBeOfType<string>();
            JToken.Parse(paramValue).Should().BeEquivalentTo(json);
        }
        else
        {
            if (value.GetType().IsEnum)
                value = Convert.ToInt32(value);
            parameter.Value.ShouldBe(value, $"Invalid value for {name}");
        }
    }

    private static bool IsJson(object? value, [MaybeNullWhen(false)] out JToken json)
    {
        if (value is not string str)
        {
            json = null;
            return false;
        }
        try
        {
            json = JToken.Parse(str);
            return true;
        }
        catch (Newtonsoft.Json.JsonException)
        {
            json = null;
            return false;
        }
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
                    Since = new LocalDate(2001, 3, 28).At(LocalTime.Noon)
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

    public static Customer[] GetTestCustomers() =>
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
            },
            Cat = new Cat()
            {
                Name = "Cat 1"
            },
            Cats = []
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
            OrderContact = null!,
            Cat = null,
            Cats = Enumerable.Range(100, 3)
                .Select(i => new Cat()
                {
                    Name = $"Cat {i}",
                })
                .ToList()
        }
    ];

    protected static ConverterEntity[] GetTestConverterEntities() =>
    [
        new ConverterEntity()
        {
            Id1 = new StringId("1"),
            Id2 = new CityId(2),
            IntValueNull = 3
        },
        new ConverterEntity()
        {
            Id1 = default,
            Id2 = new CityId(-1),
            NullableValueId = new CityId(-2),
            StringValue = "-3",
            Enum = BuildingPurpose.Commercial
        },
        new ConverterEntity()
        {
            Id1 = new StringId(string.Empty),
            ReferenceNullableIntId = new ReferenceNullableIntId(-5),
            ReferenceIntId = new ReferenceIntId(5),
            ReferenceId = new ReferenceStringId("-5")
        },
        new ConverterEntity()
        {
            ReferenceNullableIntId = new ReferenceNullableIntId(null),
            ReferenceId = new ReferenceStringId(null),
            IntValueNull = 7,
            Enum = BuildingPurpose.Governmental
        }
    ];
}