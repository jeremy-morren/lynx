using System.Data;

namespace Npgsql.BackupRestore.Tests;

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
    public async Task BackupRestore(string filename, PgBackupFormat? format)
    {
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
        
        filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", filename);
        File.Exists(filename).ShouldBeTrue();
        
        const string dbName = "efcore_test";
        DropDatabase(ConnString, dbName);

        var options = new PgRestoreOptions()
        {
            InputFile = filename,
            Format = format,
            ExitOnError = true
        };
        await PgRestore.RestoreAsync(ConnString, options, ct);
        ExecuteScalar($"{ConnString};Database={dbName}", "select count(*) from public.\"Child\"");
    }

    private static object? ExecuteScalar(string connString, string sql)
    {
        using var conn = new NpgsqlConnection(connString);
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }
    
    private static void ExecuteNonQuery(string connString, string sql)
    {
        using var conn = new NpgsqlConnection(connString);
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
    
    private static void DropDatabase(string connString, string dbName)
    {
        try
        {
            ExecuteNonQuery(connString, $"DROP DATABASE \"{dbName}\" WITH (FORCE)");
        }
        catch (NpgsqlException e) when (e.SqlState == "3D000")
        {
            // Database doesn't exist, ignore
        }
    }
}