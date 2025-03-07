using Lynx.Provider.Sqlite;

namespace Lynx.Providers.Tests.Sqlite;

public class SqliteProviderTests : ProviderTestsBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task WriteCustomers(bool useAsync)
    {
        return TestCustomers(new SqliteLynxProvider(), useAsync, _ => new SqliteTestHarness());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task WriteCities(bool useAsync)
    {
        return TestCities(new SqliteLynxProvider(), useAsync, _ => new SqliteTestHarness());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task WriteConverterEntities(bool useAsync)
    {
        return TestConverterEntities(new SqliteLynxProvider(), useAsync, _ => new SqliteTestHarness());
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task WriteIdOnly(bool useAsync)
    {
        return TestIdOnly(new SqliteLynxProvider(), useAsync, _ => new SqliteTestHarness());
    }
}