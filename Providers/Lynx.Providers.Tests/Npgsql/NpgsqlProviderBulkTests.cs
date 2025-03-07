using Lynx.Provider.Npgsql;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests.Npgsql;

public class NpgsqlProviderBulkTests : ProviderBulkTestsBase
{
    [Theory]
    [MemberData(nameof(GetFlags))]
    public Task WriteCustomers(bool useAsync, bool enableNodaTimeOnDataSource)
    {
        return TestCustomers(
            new NpgsqlLynxProvider(),
            useAsync,
            db => new NpgsqlTestHarness(
                [nameof(WriteCustomers), db, useAsync, enableNodaTimeOnDataSource ],
                enableNodaTimeOnDataSource));
    }
    //
    // [Theory]
    // [MemberData(nameof(GetFlags))]
    // public Task WriteCities(bool useAsync, bool enableNodaTimeOnDataSource)
    // {
    //     return TestCities(
    //         new NpgsqlLynxProvider(),
    //         useAsync,
    //         db => new NpgsqlTestHarness(
    //             [nameof(WriteCities), db, useAsync, enableNodaTimeOnDataSource ],
    //             enableNodaTimeOnDataSource));
    // }
    //
    // [Theory]
    // [MemberData(nameof(GetFlags))]
    // public Task WriteConverterEntities(bool useAsync, bool enableNodaTimeOnDataSource)
    // {
    //     return TestConverterEntities(
    //         new NpgsqlLynxProvider(),
    //         useAsync,
    //         db => new NpgsqlTestHarness(
    //             [nameof(WriteConverterEntities), db, useAsync, enableNodaTimeOnDataSource ],
    //             enableNodaTimeOnDataSource));
    // }

    public static TheoryData<bool, bool> GetFlags() => new()
    {
        // parameters: useAsync, enableNodaTimeOnDataSource
        { false, false },
        // { false, true },
        // { true, false },
        // { true, true }
    };
}