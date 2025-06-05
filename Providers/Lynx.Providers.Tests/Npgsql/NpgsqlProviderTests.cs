using Lynx.Provider.Npgsql;

namespace Lynx.Providers.Tests.Npgsql;

public class NpgsqlProviderTests : ProviderTestsBase
{
    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCustomers(ProviderTestType type)
    {
        return TestCustomers(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(GetDatabase(nameof(WriteCustomers), db, type)));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCities(ProviderTestType type)
    {
        return TestCities(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(GetDatabase(nameof(WriteCities), db, type)));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteConverterEntities(ProviderTestType type)
    {
        return TestConverterEntities(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(
                GetDatabase(nameof(WriteConverterEntities), db, type)));
    }

    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteIdOnly(ProviderTestType type)
    {
        return TestIdOnly(
            new NpgsqlLynxProvider(),
            type,
            db => new NpgsqlTestHarness(GetDatabase(nameof(TestIdOnly), db, type)));
    }

    public static TheoryData<ProviderTestType> GetFlags()
    {
        var data = new TheoryData<ProviderTestType>();
        foreach (var type in Enum.GetValues<ProviderTestType>())
            data.Add(type);
        return data;
    }

    private static object[] GetDatabase(string name, string db, ProviderTestType type) =>
        [nameof(NpgsqlProviderTests), name, db, type];
}