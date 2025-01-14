namespace Npgsql.BackupRestore.Tests;

public class PgBackupRestoreTests : PgToolTestsBase
{
    [Theory]
    [MemberData(nameof(GetBackupOptions))]
    public async Task RoundTrip(PgBackupOptions options)
    {
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;
        
        var database = Guid.NewGuid().ToString("N");
        CleanDatabase(database);
        
        var path = Path.GetTempFileName();
        DeleteFileOrDirectory(path);
        options.FileName = path;
        
        try
        {
            await PgBackup.BackupAsync(ConnString, options, Database, ct);

            if (options.Format != PgBackupFormat.Directory)
                new FileInfo(path).Exists.ShouldBeTrue();
            else
                Directory.Exists(path).ShouldBeTrue();

            if (options.Format == PgBackupFormat.Plain)
            {
                // We should be able to execute the SQL file directly
                var sql = await File.ReadAllTextAsync(path, ct);
                ExecuteNonQuery($"{ConnString};Database={database}", sql);
            }
            else
            {
                // For other formats, restore via pg_restore
                var restoreOpts = new PgRestoreOptions()
                {
                    Database = database,
                    Clean = true,
                    IfExists = true
                };
                await PgRestore.RestoreAsync(ConnString, restoreOpts, path, ct);
            }

            ExecuteScalar($"{ConnString};Database={database}", 
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_type = 'BASE TABLE'")
                .ShouldBeOfType<long>().ShouldBePositive();

            DropDatabase(database);
        }
        finally
        {
            DeleteFileOrDirectory(path);
        }
    }

    public static TheoryData<PgBackupOptions> GetBackupOptions() => new()
    {
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Plain,
            NoOwner = true,
            NoPrivileges = true,
            Inserts = true,
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Plain,
            ColumnInserts = true,
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Custom,
            Compression = 3,
            IncludeBlobs = true,
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Tar,
            Schema = GetSchemaWithTables(),
        },
        new PgBackupOptions()
        {
            Format = PgBackupFormat.Directory,
            Compression = 5,
            Schema = GetSchemaWithTables(),
        }
    };
}