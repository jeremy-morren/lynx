using Lynx.Provider.Common;
using Lynx.Provider.Common.Entities;
using Lynx.Providers.Tests.Sqlite;

namespace Lynx.Providers.Tests;

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
        CommandGenerator.GetInsertWithKeyCommand(entity).ShouldEndWith(")");

        if (entity.Keys.Count == 1)
            CommandGenerator.GetInsertIdentityCommand(entity).ShouldNotEndWith(")");
        else
            Assert.Throws<InvalidOperationException>(() => CommandGenerator.GetInsertIdentityCommand(entity));

        CommandGenerator.GetUpsertCommand(entity)
            .Should().NotEndWith(")").And.Contain("ON CONFLICT");
    }
}