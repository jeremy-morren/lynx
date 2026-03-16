using Lynx.Provider.SqlServer;

namespace Lynx.Providers.Tests.SqlServer;

public class SqlServerCommandGeneratorTests
{
    [Theory]
    [InlineData(typeof(Customer), "[Id] = @Id")]
    [InlineData(typeof(City), "[Id] = @Id")]
    public void GenerateUpsertCommand_UsesUpdateThenInsert(Type entityType, string keyPredicate)
    {
        using var harness = new SqlServerTestHarness([nameof(GenerateUpsertCommand_UsesUpdateThenInsert), entityType.Name]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactoryTests.CreateRootEntity(entityType, context.Model);
        var generator = new SqlServerCommandGenerator(entity);

        var command = generator.GetUpsertCommand();

        command.ShouldContain("UPDATE ");
        command.ShouldContain("SET ");
        command.ShouldContain($"WHERE {keyPredicate};");
        command.ShouldContain("IF @@ROWCOUNT = 0");
        command.ShouldContain("BEGIN");
        command.ShouldContain("INSERT INTO ");
        command.ShouldNotContain("ON CONFLICT");
    }

    [Fact]
    public void GenerateUpsertCommand_UsesAndForCompositeKeys()
    {
        using var harness = new SqlServerTestHarness([nameof(GenerateUpsertCommand_UsesAndForCompositeKeys)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactoryTests.CreateRootEntity(typeof(ConverterEntity), context.Model);
        var generator = new SqlServerCommandGenerator(entity);

        var command = generator.GetUpsertCommand();

        command.ShouldContain("WHERE [Id1] = @Id1 AND [Id2] = @Id2;");
        command.ShouldNotContain("[Id1] = @Id1, [Id2] = @Id2");
    }

    [Fact]
    public void GenerateUpsertCommand_UsesIfNotExistsForKeyOnlyEntities()
    {
        using var harness = new SqlServerTestHarness([nameof(GenerateUpsertCommand_UsesIfNotExistsForKeyOnlyEntities)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactoryTests.CreateRootEntity(typeof(IdOnly), context.Model);
        var generator = new SqlServerCommandGenerator(entity);

        var command = generator.GetUpsertCommand();

        command.ShouldStartWith("IF NOT EXISTS (SELECT 1 FROM [IdOnly] WHERE [Id] = @Id)");
        command.ShouldContain("INSERT INTO [IdOnly] ([Id]) VALUES (@Id);");
        command.ShouldNotContain("UPDATE [IdOnly]");
        command.ShouldNotContain("IF @@ROWCOUNT = 0");
        command.ShouldNotContain("ON CONFLICT");
    }
}
