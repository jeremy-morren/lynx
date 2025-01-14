using System.Data;

namespace Npgsql.BackupRestore.Tests;

[Collection("PgRestoreTests")]
public class PgRestoreTests : PgToolTestsBase
{
    [Fact]
    public void AllOptionNamesShouldBeValid()
    {
        PgToolFinder.FindPgTool(PgRestore.ToolName).ShouldNotBeEmpty();
        
        //Ensure all option names are valid
        var optionNames = GetOptionNames(PgToolFinder.FindPgTool(PgRestore.ToolName)[0]);
        Assert.All(PgRestore.OptionNames.Values, k => optionNames.ShouldContain(k));
    }
    
    [Theory]
    [InlineData("Backup.bin", PgBackupFormat.Custom)]
    [InlineData("Backup.tar", PgBackupFormat.Tar)]
    [InlineData("Backup.bin", null)]
    [InlineData("Backup.tar", null)]
    public async Task RestoreFile(string filename, PgBackupFormat? format)
    {
        var database = $"{nameof(RestoreFile)}_{filename.Replace('.', '_')}_{format}".ToLowerInvariant();
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        
        filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", filename);
        File.Exists(filename).ShouldBeTrue();

        CleanDatabase(database);

        var options = new PgRestoreOptions()
        {
            Database = database,
            Format = format,
            ExitOnError = true
        };
        await PgRestore.RestoreAsync(ConnString, options, filename, ct);

        ExecuteScalar($"{ConnString};Database={database}", "select count(*) from public.\"Child\"").ShouldBe(3);
    }
    
    [Theory]
    [InlineData("Backup.bin", PgBackupFormat.Custom)]
    [InlineData("Backup.tar", PgBackupFormat.Tar)]
    [InlineData("Backup.bin", null)]
    [InlineData("Backup.tar", null)]
    public async Task RestoreStream(string filename, PgBackupFormat? format)
    {
        var database = $"{nameof(RestoreFile)}_{filename.Replace('.', '_')}_{format}".ToLowerInvariant();
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        
        filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", filename);
        File.Exists(filename).ShouldBeTrue();

        CleanDatabase(database);

        var options = new PgRestoreOptions()
        {
            Database = database,
            Format = format,
            ExitOnError = true
        };
        await using var fs = File.OpenRead(filename);
        await PgRestore.RestoreAsync(ConnString, options, fs, ct);

        ExecuteScalar($"{ConnString};Database={database}", "select count(*) from public.\"Child\"").ShouldBe(3);
    }
}