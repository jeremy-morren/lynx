using System.Text;
using Npgsql.BackupRestore.DataDump;

// ReSharper disable MethodHasAsyncOverload

namespace Npgsql.BackupRestore.Tests;

public class PgDataDumpTests : PgToolTestsBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetTables(bool async)
    {
        await using var connection = new NpgsqlConnection(ConnString);
        var tables = async
            ? await PgTableReader.GetTablesAsync(connection, default)
            : PgTableReader.GetTables(connection);
        tables.Should().NotBeEmpty();
        tables.ShouldAllBe(t => t.Columns.Count > 0);
        tables.OrderBy(x => x.Dependencies.Count)
            .ThenBy(x => x.Schema)
            .ThenBy(x => x.Table)
            .Should().BeEquivalentTo(tables);
    }

    [Theory]
    [InlineData(8000, 8000)]
    [InlineData(1024, 2000)]
    [InlineData(1024, 520)]
    [InlineData(1024, 300)]
    public async Task RoundTrip(short segmentSize, int bufferSize)
    {
        var schema = await DumpSchemaAsync();
        var data = await DumpDatabase(Database);
        foreach (var async in new[] { false, true })
        {
            using var ms = new MemoryStream();
            if (async)
                await PgDataDump.BackupAsync(ConnString, ms, segmentSize);
            else
                PgDataDump.Backup(ConnString, ms, segmentSize);
            ms.Length.ShouldBePositive();

            var outDatabase = $"{nameof(RoundTrip)}_{async}_{segmentSize}_{bufferSize}";
            try
            {
                CleanDatabase(outDatabase);
                var outConnString = $"{MasterConnString};Database={outDatabase}";
                ExecuteScalar(outConnString, schema);

                ms.Position = 0;
                if (async)
                    await PgDataDump.RestoreAsync(outConnString, ms, bufferSize);
                else
                    PgDataDump.Restore(outConnString, ms, bufferSize);

                // Ensure that the restored database has same data as original
                (await DumpDatabase(outDatabase)).ShouldBeEquivalentTo(data);

                // Backing up the restored database should be equivalent to original
                using var finalBackup = new MemoryStream();
                if (async)
                    await PgDataDump.BackupAsync(outConnString, finalBackup, segmentSize);
                else
                    PgDataDump.Backup(outConnString, finalBackup, segmentSize);
                finalBackup.ToArray().ShouldBeEquivalentTo(ms.ToArray());
            }
            finally
            {
                DropDatabase(outDatabase);
            }
        }
    }

    private static async Task<string> DumpSchemaAsync()
    {
        var options = new PgBackupOptions()
        {
            SchemaOnly = true,
            Format = PgBackupFormat.Plain,
            NoOwner = true,
            NoPrivileges = true
        };
        using var ms = new MemoryStream();
        await PgBackup.BackupAsync(MasterConnString, options, Database, ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static async Task<byte[]> DumpDatabase(string database)
    {
        var options = new PgBackupOptions()
        {
            Format = PgBackupFormat.Plain,
            NoOwner = true,
            NoPrivileges = true
        };
        using var ms = new MemoryStream();
        await PgBackup.BackupAsync(MasterConnString, options, Database, ms);
        return ms.ToArray();
    }
}