using Lynx.Provider.Common;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Sqlite;

namespace Lynx.Providers.Tests.Sqlite;

public class CommandGeneratorTests
{
    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(City))]
    public void GenerateCommands(Type entityType)
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(entityType, context.Model);
        SqliteCommandGenerator.GetInsertWithKeyCommand(entity).ShouldEndWith(")");

        if (entity.Keys.Count == 1)
            SqliteCommandGenerator.GetInsertIdentityCommand(entity).ShouldNotEndWith(")");
        else
            Assert.Throws<InvalidOperationException>(() => SqliteCommandGenerator.GetInsertIdentityCommand(entity));

        SqliteCommandGenerator.GetUpsertCommand(entity)
            .Should().NotEndWith(")").And.Contain("ON CONFLICT");
    }
}