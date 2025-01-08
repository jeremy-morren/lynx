using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Lynx.NpgsqlBackupRestore;
using Xunit.Abstractions;

namespace Lynx.Tests;

[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
public class NpgsqlBackupRestoreTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("psql", "16.6")]
    [InlineData("pg_restore", "15.2.6")]
    [InlineData("pg_dump", "14.2.8.4")]
    public void FindTool(string tool, string version)
    {
        PgToolFinder.FindPgTool(tool).ShouldNotBeEmpty();

        var paths = PgToolFinder.FindPgTool(tool)
            .Select(p => new
            {
                Path = p,
                Version = PgToolFinder.GetToolVersion(p)
            })
            .ToList();
        
        foreach (var path in paths)
            output.WriteLine($"{path.Path}: '{path.Version}'");

        // var minVersion = Version.Parse(version);
        // var path = NpgsqlBackupRestore.PgToolFinder.FindPgTool(tool, minVersion);
        // path.ShouldNotBeNull();
        // var fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion.ShouldNotBeNull();
        // Version.Parse(fileVersion).ShouldBeGreaterThanOrEqualTo(minVersion);
    }
    
    [Theory]
    [InlineData("pg_restore (PostgreSQL) 16.6 (Debian 16.6-1.pgdg120+1)", "16.6")]
    [InlineData("psql (PostgreSQL) 15.2.6", "15.2.6")]
    public void GetToolVersion(string versionString, string expected)
    {
        PgToolFinder.PgToolVersionRegex().Match(versionString).Value.ShouldBe(expected);
    }
}