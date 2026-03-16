using System.Data.Common;
using Lynx.Provider.SqlServer;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Lynx.Providers.Tests.SqlServer;

public class SqlServerProviderTests : ProviderTestsBase
{
    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCustomers(ProviderTestType type)
    {
        return TestCustomers(
            new SqlServerLynxProvider(),
            type,
            db => new SqlServerTestHarness(GetDatabase(nameof(WriteCustomers), db, type)));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCities(ProviderTestType type)
    {
        return TestCities(
            new SqlServerLynxProvider(),
            type,
            db => new SqlServerTestHarness(GetDatabase(nameof(WriteCities), db, type)));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteConverterEntities(ProviderTestType type)
    {
        return TestConverterEntities(
            new SqlServerLynxProvider(),
            type,
            db => new SqlServerTestHarness(
                GetDatabase(nameof(WriteConverterEntities), db, type)));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteIdOnly(ProviderTestType type)
    {
        return TestIdOnly(
            new SqlServerLynxProvider(),
            type,
            db => new SqlServerTestHarness(
                GetDatabase(nameof(TestIdOnly), db, type)));
    }

    public static TheoryData<ProviderTestType> GetFlags()
    {
        var data = new TheoryData<ProviderTestType>();
        foreach (var type in Enum.GetValues<ProviderTestType>())
            data.Add(type);
        return data;
    }

    private static object[] GetDatabase(string name, string db, ProviderTestType type) =>
        [nameof(SqlServerProviderTests), name, db, type];

    protected override void SetIdentityInsertOn<T>(DbContext context, DbTransaction transaction)
    {
        var entity = EntityInfoFactory.CreateRoot<T>(context.Model);
        var sql = new SqlServerCommandGenerator(entity)
            .GetSetIdentityInsertCommand(true);
        if (sql == null)
            return;

        transaction.Connection.ShouldNotBeNull();
        using var _ = OpenConnection.Open(transaction.Connection);
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}