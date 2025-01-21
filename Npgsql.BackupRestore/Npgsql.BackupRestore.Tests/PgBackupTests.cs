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
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        var file = Path.GetTempFileName();
        DeleteFileOrDirectory(file);

        options.FileName = file;
        try
        {
            await PgBackup.BackupAsync(ConnString,  options, Database, ct);
            new FileInfo(file).Exists.ShouldBeTrue();
            new FileInfo(file).Length.ShouldBePositive();
            
            options.FileName = null;
            using var ms = new MemoryStream();
            await PgBackup.BackupAsync(ConnString, options, Database, ms, ct);
            ms.Length.ShouldBe(new FileInfo(file).Length);
            
            if (options.SchemaOnly)
                ms.ToArray().ShouldBeEquivalentTo(await File.ReadAllBytesAsync(file, ct));
            
            var kb = new FileInfo(file).Length / 1024d;
            output.WriteLine($"{kb:#,0.##} KiB");
        }
        finally
        {
            DeleteFileOrDirectory(file);
        }
    }

    public static TheoryData<PgBackupOptions> GetBackupOptions() => new()
    {
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Plain,
            Compression = "4",
            NoOwner = true,
            NoPrivileges = true,
            SchemaOnly = true,
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Custom,
            Schema = GetSchemaWithTables(),
            DataOnly = true
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Tar,
            Schema = GetSchemaWithTables()
        }
    };
}