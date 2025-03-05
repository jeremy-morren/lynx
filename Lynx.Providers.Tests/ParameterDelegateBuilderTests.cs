using System.Data.Common;
using System.Linq.Expressions;
using FluentAssertions;
using Lynx.Provider.Common;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

        var action = AddParameterDelegateBuilder<SqliteCommand>.Build(entity);

        var command = new SqliteCommand();
        action(command);
        command.Parameters.Count.ShouldBe(entity.GetAllScalarProps().Count() + entity.Keys.Count);
    }

    [Fact]
    public void Test()
    {
        Expression<Func<DbCommand, object>> action = cmd => cmd.Parameters[0].Value;
    }
    
    [Fact]
    public void BuildSetParametersDelegate()
    {
        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(typeof(City), context.Model);

        var addParameters = AddParameterDelegateBuilder<SqliteCommand>.Build(entity);

        var command = new SqliteCommand();
        addParameters(command);
        
        var setParameters = SetParameterValueDelegateBuilder<SqliteCommand, City>.Build(entity);
    }
}