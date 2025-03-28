using Lynx.Providers.Common;
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

        var entity = RootEntityInfoFactoryTests.CreateRootEntity(entityType, context.Model);
        var generator = new CommandGenerator(entity);
        generator.GetInsertWithKeyCommand().ShouldEndWith(")");

        if (entity.Keys.Count == 1)
            generator.GetInsertIdentityCommand().ShouldNotEndWith(")");
        else
            Assert.Throws<InvalidOperationException>(() => generator.GetInsertIdentityCommand());

        generator.GetUpsertCommand().Should().NotEndWith(")").And.Contain("ON CONFLICT");
    }
}