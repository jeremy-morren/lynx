using Lynx.Provider.Npgsql;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests.Npgsql;

public class NpgsqlProviderTests : ProviderTestsBase
{
    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCustomers(bool useAsync, bool enableNodaTimeOnDataSource)
    {
        return TestCustomers(
            new NpgsqlLynxProvider(),
            useAsync,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteCustomers), db, useAsync, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCities(bool useAsync, bool enableNodaTimeOnDataSource)
    {
        return TestCities(
            new NpgsqlLynxProvider(),
            useAsync,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteCities), db, useAsync, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteConverterEntities(bool useAsync, bool enableNodaTimeOnDataSource)
    {
        return TestConverterEntities(
            new NpgsqlLynxProvider(),
            useAsync,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteConverterEntities), db, useAsync, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteIdOnly(bool useAsync, bool enableNodaTimeOnDataSource)
    {
        return TestIdOnly(
            new NpgsqlLynxProvider(),
            useAsync,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(TestIdOnly), db, useAsync, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    public static TheoryData<bool, bool> GetFlags() => new()
    {
        // parameters: useAsync, enableNodaTimeOnDataSource
        { false, false },
        { false, true },
        { true, false },
        { true, true }
    };
    
    
    private static object[] GetDatabase(string name, string db, bool useAsync, bool enableNodaTimeOnDataSource) =>
        [nameof(NpgsqlProviderTests), name, db, useAsync, enableNodaTimeOnDataSource];
}