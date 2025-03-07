using System.Collections.Immutable;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Npgsql;

namespace Lynx.Providers.Tests.Npgsql;

public class NpgsqlEntityColumnBuilderTests : ParameterDelegateBuilderTestsBase
{
    [Fact]
    public void BuildCitySetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildCitySetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(typeof(City), context.Model);

        var columns = NpgsqlEntityColumnBuilder<City>.GetColumnInfo(entity);

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
            Verify([nameof(City.Buildings)], city.Buildings);

            return;

            void Verify(ImmutableArray<string> property, object? value)
            {
                var column = columns.Where(c => c.Property.Name == property).ShouldHaveSingleItem();
                column.GetValue(city).ShouldBe(value, $"Invalid value for column {column.Property.Name}");
            }
        });
    }
}