using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore;

public static class PgRestore
{
    public static async Task RestoreAsync(
        string connectionString, 
        PgRestoreOptions options, 
        string filename,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(filename);
        
        connectionString = PgBackup.PersistSecurityInfo(connectionString);

        await using var connection = new NpgsqlConnection(connectionString);
        await RestoreAsync(connection, options, filename, cancellationToken);
    }
    
    public static async Task RestoreAsync(
        NpgsqlConnection connection, 
        PgRestoreOptions options, 
        string filename,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(filename);
        
        var version = await NpgsqlServerHelpers.GetServerVersion(connection, cancellationToken);
        var tool = PgToolFinder.FindPgTool(ToolName, version);
        var args = CommandHelpers.GetArgs(options, OptionNames).Append(filename).ToList();
        var env = CommandHelpers.GetEnvVariables(connection.ConnectionString);
        await LongCmdRunner.RunAsync(tool, args, env, null, null, cancellationToken);
    }
    
    internal const string ToolName = "pg_restore";
    
    internal static readonly Dictionary<string, string> OptionNames = new()
    {
        { nameof(PgRestoreOptions.Database), "--dbname" },
        { nameof(PgRestoreOptions.Format), "--format" },
        { nameof(PgRestoreOptions.DataOnly), "--data-only" },
        { nameof(PgRestoreOptions.Clean), "--clean" },
        { nameof(PgRestoreOptions.Create), "--create" },
        { nameof(PgRestoreOptions.ExitOnError), "--exit-on-error" },
        { nameof(PgRestoreOptions.Index), "--index" },
        { nameof(PgRestoreOptions.Schema), "--schema" },
        { nameof(PgRestoreOptions.ExcludeSchema), "--exclude-schema" },
        { nameof(PgRestoreOptions.Table), "--table" },
        { nameof(PgRestoreOptions.Trigger), "--trigger" },
        { nameof(PgRestoreOptions.Function), "--function" },
        { nameof(PgRestoreOptions.NoOwner), "--no-owner" },
        { nameof(PgRestoreOptions.NoPrivileges), "--no-privileges" },
        { nameof(PgRestoreOptions.SingleTransaction), "--single-transaction" },
        { nameof(PgRestoreOptions.Jobs), "--jobs" },
        { nameof(PgRestoreOptions.Verbose), "--verbose" },
    };
}