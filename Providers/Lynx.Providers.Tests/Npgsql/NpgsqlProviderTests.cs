using Lynx.Provider.Npgsql;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests.Npgsql;

public class NpgsqlProviderTests : ProviderTestsBase
{
    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCustomers(ProviderTestType type, bool enableNodaTimeOnDataSource)
    {
        return TestCustomers(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteCustomers), db, type, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCities(ProviderTestType type, bool enableNodaTimeOnDataSource)
    {
        return TestCities(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteCities), db, type, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteConverterEntities(ProviderTestType type, bool enableNodaTimeOnDataSource)
    {
        return TestConverterEntities(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteConverterEntities), db, type, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteIdOnly(ProviderTestType type, bool enableNodaTimeOnDataSource)
    {
        return TestIdOnly(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(TestIdOnly), db, type, enableNodaTimeOnDataSource ),
                enableNodaTimeOnDataSource));
    }

    public static TheoryData<ProviderTestType, bool> GetFlags()
    {
        // parameters: type, enableNodaTimeOnDataSource
        var data = new TheoryData<ProviderTestType, bool>();
        foreach (var type in Enum.GetValues<ProviderTestType>())
        {
            data.Add(type, false);
            data.Add(type, true);
        }

        return data;
    }

    private static object[] GetDatabase(string name, string db, ProviderTestType type, bool enableNodaTimeOnDataSource) =>
        [nameof(NpgsqlProviderTests), name, db, type, enableNodaTimeOnDataSource];
}