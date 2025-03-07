using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Npgsql;

namespace Lynx.Providers.Tests.Npgsql;

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

    private static void CommandBuilderTestsGeneric<T>(TestContext context)
    {
        var entity = EntityInfoFactory.Create(typeof(T), context.Model);
        var expected = entity.Keys.Concat(entity.GetAllScalarColumns()).ToList();

        var columns = NpgsqlEntityColumnBuilder<T>.GetColumnInfo(entity);
        columns.Select(c => c.Property.Name)
            .Should().BeEquivalentTo(expected.Select(c => c.Name));

        var generator = new NpgsqlCommandGenerator(entity);
        var commands = new[]
        {
            generator.GenerateBinaryCopyInsertCommand(),
            generator.GenerateBinaryCopyTempTableInsertCommand()
        };
        Assert.All(commands, command =>
        {
            Regex.Matches(command, @"(?<=[,\(]\W*?"")\w+(?=""[,\)])")
                .Select(m => m.Value)
                .Should()
                .BeEquivalentTo(columns.Select(c => c.Property.ColumnName.SqlColumnName),
                    "Columns in the command should match the entity columns");
        });

        generator.GetCreateTempTableCommand().ShouldContain(generator.TempTableName);
        generator.GenerateUpsertTempTableCommand().Should().Contain(generator.TempTableName)
            .And.Contain(generator.QualifiedTableName);
    }

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