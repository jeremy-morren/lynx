using System.Collections.Immutable;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text.Json;
using FluentAssertions;
using Lynx.Provider.Common;
using Lynx.Provider.Sqlite;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.Data.Sqlite;
using Shouldly;

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
    public void BuildSetParametersDelegate()
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

            VerifyParameter([nameof(City.Id)], city.Id.Value);
            VerifyParameter([nameof(City.Name)], city.Name);
            VerifyParameter([nameof(City.Country)], city.Country);
            VerifyParameter([nameof(City.Population)], city.Population);
            VerifyParameter([nameof(City.Location), nameof(CityLocation.Latitude)], city.Location.Latitude);
            VerifyParameter([nameof(City.Location), nameof(CityLocation.Longitude)], city.Location.Longitude);
            VerifyParameter([nameof(City.Location), nameof(CityLocation.Elevation)], city.Location.Elevation);
            VerifyParameter([nameof(City.LegalSystem), nameof(LegalSystem.CommonLaw)], city.LegalSystem.CommonLaw);
            VerifyParameter([nameof(City.LegalSystem), nameof(LegalSystem.CivilLaw)], city.LegalSystem.CivilLaw);
            VerifyParameter([nameof(City.FamousBuilding), nameof(Building.Name)], city.FamousBuilding?.Name);
            VerifyParameter([nameof(City.FamousBuilding), nameof(Building.Purpose)], city.FamousBuilding?.Purpose);
            VerifyParameter([nameof(City.Buildings)], JsonSerializer.Serialize(city.Buildings));

            command.Parameters.Count.ShouldBe(entity.GetAllScalarColumns().Count() + entity.Keys.Count);
        });

        return;

        void VerifyParameter(ImmutableArray<string> property, object? value)
        {
            var name = string.Join(".", property);

            var scalar = entity.Keys.Concat(entity.GetAllScalarColumns())
                .Where(e => e.Name == property)
                .ShouldHaveSingleItem($"Property {name} not found");

            var parameter = command.Parameters[scalar.ColumnName.SqlParamName];
            if (value == null)
                parameter.Value.ShouldBe(DBNull.Value, $"{name} should be DBNull.Value");
            else
                parameter.Value.ShouldBe(value, $"Invalid value for {name}");
        }
    }

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
                Purpose = BuildingPurpose.Governmental
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
            Location = new CityLocation()
            {
                Latitude = 49.5m,
                Longitude = 12.8,
                Elevation = 20
            },
            FamousBuilding = new Building()
            {
                Name = null!,
                Purpose = (BuildingPurpose)75
            }
        }
    ];
}