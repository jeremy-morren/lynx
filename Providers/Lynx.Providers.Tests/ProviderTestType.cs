namespace Lynx.Providers.Tests;

public enum ProviderTestType
{
    /// <summary>
    /// Use sync methods
    /// </summary>
    Sync,

    /// <summary>
    /// Use async methods with sync enumerable
    /// </summary>
    Async,

    /// <summary>
    /// Use async enumerable
    /// </summary>
    AsyncEnumerable
}