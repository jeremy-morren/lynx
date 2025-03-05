using FluentAssertions;
using Lynx.Provider.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Lynx.Providers.Tests;

public class AddParameterDelegateBuilderTests
{
    [Fact]
    public void BuildCityDelegate()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(nameof(BuildCityDelegate))
            .Options;

        using var context = new TestContext(options);

        var entity = EntityInfoFactory.Create(typeof(City), context.Model);

        var action = AddParameterDelegateBuilder<City>.Build(entity);

        var city = new City
        {
            Id = new CityId(1),
            Name = "New York",
            Location = new CityLocation()
            {
                Elevation = 5
            }
        };
        var command = new SqliteCommand();
        action(command, city);
        command.Parameters.Count.ShouldBe(entity.GetAllProperties().Count() + 1);
    }

}