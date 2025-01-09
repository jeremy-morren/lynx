using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Npgsql.BackupRestore.Commands;
using Xunit.Abstractions;

namespace Npgsql.BackupRestore.Tests;

public class PgBackupTests(ITestOutputHelper output) : PgToolTestsBase
{
    [Fact]
    public void AllOptionNamesShouldBeValid()
    {
        PgToolFinder.FindPgTool(PgBackup.ToolName).ShouldNotBeEmpty();
        
        //Ensure all option names are valid
        var optionNames = GetOptionNames(PgToolFinder.FindPgTool(PgBackup.ToolName)[0]);
        Assert.All(PgBackup.OptionNames.Values, k => optionNames.ShouldContain(k));
    }

    [Theory]
    [MemberData(nameof(GetBackupOptions))]
    public async Task Backup(PgBackupOptions options)
    {
        var file = Path.GetTempFileName();
        DeleteFile(file);

        options.FileName = file;
        try
        {
            await PgBackup.BackupAsync(ConnString, options);
            new FileInfo(file).Exists.ShouldBeTrue();
            new FileInfo(file).Length.ShouldBePositive();

            options.FileName = null;
            using var ms = new MemoryStream();
            await PgBackup.BackupAsync(ConnString, options, ms);
            ms.Length.ShouldBe(new FileInfo(file).Length);
            
            var kb = new FileInfo(file).Length / 1024d;
            output.WriteLine($"{kb:#,0.##} KiB");
        }
        finally
        {
            DeleteFile(file);
        }
    }

    public static TheoryData<PgBackupOptions> GetBackupOptions() => new()
    {
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Plain,
            Compression = 4,
            NoOwner = true,
            NoPrivileges = true,
            SchemaOnly = true,
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Custom,
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Tar,
            Schema = "public"
        },
    };
}