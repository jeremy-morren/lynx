namespace Lynx.Providers.Tests;

public interface ITestHarness : IDisposable
{
    TestContext CreateContext();
}