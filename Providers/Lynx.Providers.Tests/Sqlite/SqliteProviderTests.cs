using Lynx.Provider.Sqlite;

namespace Lynx.Providers.Tests.Sqlite;

public class SqliteProviderTests : ProviderTestsBase
{
    [Theory]
    [MemberData(nameof(GetTypes))]
    public Task WriteCustomers(ProviderTestType type)
    {
        return TestCustomers(new SqliteLynxProvider(), type, _ => new SqliteTestHarness());
    }

    [Theory]
    [MemberData(nameof(GetTypes))]
    public Task WriteCities(ProviderTestType type)
    {
        return TestCities(new SqliteLynxProvider(), type, _ => new SqliteTestHarness());
    }

    [Theory]
    [MemberData(nameof(GetTypes))]
    public Task WriteConverterEntities(ProviderTestType type)
    {
        return TestConverterEntities(new SqliteLynxProvider(), type, _ => new SqliteTestHarness());
    }
    
    [Theory]
    [MemberData(nameof(GetTypes))]
    public Task WriteIdOnly(ProviderTestType type)
    {
        return TestIdOnly(new SqliteLynxProvider(), type, _ => new SqliteTestHarness());
    }

    public static TheoryData<ProviderTestType> GetTypes() => new(Enum.GetValues<ProviderTestType>());
}