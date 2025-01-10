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
        
        var file = Path.GetTempFileName();
        DeleteFile(file);
        options.FileName = file;
        
        try
        {
            await PgBackup.BackupAsync(ConnString, options, Database, ct);
            new FileInfo(file).Exists.ShouldBeTrue();

            if (options.Format == PgBackupFormat.Plain)
            {
                // We should be able to execute the SQL file directly
                var sql = await File.ReadAllTextAsync(file, ct);
                ExecuteNonQuery($"{ConnString};Database={database}", sql);
            }
            else
            {
                // For other formats, restore via pg_restore
                var restoreOpts = new PgRestoreOptions()
                {
                    Database = database,
                };
                await PgRestore.RestoreAsync(ConnString, restoreOpts, file, ct);
            }

            ExecuteScalar($"{ConnString};Database={database}", 
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_type = 'BASE TABLE'")
                .ShouldBeOfType<long>().ShouldBePositive();
        }
        finally
        {
            DeleteFile(file);
            DropDatabase(database);
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
            Schema = GetSchemaWithTables()
        }
    };
}