using System.Diagnostics.CodeAnalysis;
using Xunit.Abstractions;

namespace Npgsql.BackupRestore.Tests;

[SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
public class NpgsqlBackupRestoreTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("psql")]
    [InlineData("pg_restore")]
    [InlineData("pg_dump")]
    public void FindTool(string tool)
    {
        PgToolFinder.FindPgTool(tool).ShouldNotBeEmpty();

        Assert.All(PgToolFinder.FindPgTool(tool), path =>
        {
            File.Exists(path).ShouldBeTrue();
            var version = PgToolFinder.GetToolVersion(path).ShouldNotBeNull();
            
            //Note: Getting the tool for a version may return a different path
            //e.g. on linux, /var/lib/postgresql/13/bin/psql may become /usr/bin/psql
            //So we can't check the return value matches path directly
            
            //Get matching version should succeed
            VerifyVersion(PgToolFinder.FindPgTool(tool, version), version);

            if (version.Minor > 0)
            {
                var v = new Version(version.Major, version.Minor - 1);
                VerifyVersion(PgToolFinder.FindPgTool(tool, v), version);
            }
            version = new Version(version.Major, version.Minor + 1);
            var ex = Assert.Throws<InvalidOperationException>(() => PgToolFinder.FindPgTool(tool, version));
            ex.Message.ShouldContain($"{version.Major}.*");
            
            output.WriteLine($"{path}: {version}");
        });

        return;
        
        void VerifyVersion(string path, Version expected)
        {
            PgToolFinder.GetToolVersion(path).ShouldNotBeNull().ShouldBe(expected);
        }
    }
    
    [Theory]
    [InlineData("pg_restore (PostgreSQL) 16.6 (Debian 16.6-1.pgdg120+1)", "16.6")]
    [InlineData("psql (PostgreSQL) 15.2.6", "15.2.6")]
    public void GetToolVersion(string versionString, string expected)
    {
        PgToolFinder.PgToolVersionRegex().Match(versionString).Value.ShouldBe(expected);
    }
}