using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lynx.Tests;

public static class DatabaseNameHelpers
{
    /// <summary>
    /// Gets a unique database name for a test based on the test method name, arguments, and runtime identifier.
    /// </summary>
    public static string GetDatabaseName([CallerMemberName] string? callerName = null) =>
        GetDatabaseName([], callerName);
    
    /// <summary>
    /// Gets a unique database name for a test based on the test method name, arguments, and runtime identifier.
    /// </summary>
    public static string GetDatabaseName(object[] args, [CallerMemberName] string? callerName = null)
    {
        callerName.ShouldNotBeNullOrEmpty();
        return $"{callerName}_{string.Join("_", args)}_{GetFrameworkName()}".ToLowerInvariant();
    }
    
    private static string GetFrameworkName() => RuntimeInformation.FrameworkDescription.Replace(".NET ", "").Replace(".","_");
}