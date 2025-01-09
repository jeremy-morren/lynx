using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore;

public static class PgBackup
{
    /// <summary>
    /// Runs a pg_dump command to backup a PostgreSQL database to a file
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(
        string connectionString,
        PgBackupOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        
        if (options.FileName == null)
            throw new ArgumentException("FileName must be set", nameof(options));
        
        var version = await NpgsqlServerHelpers.GetServerVersion(connectionString, cancellationToken);
        // Get pg_dump that matches the server version
        var pgDump = PgToolFinder.FindPgTool(ToolName, version);
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        
        var args = CommandHelpers.GetArgs(options, OptionNames).ToList();
        var env = CommandHelpers.GetEnvVariables(builder);
        await LongCmdRunner.RunAsync(pgDump, args, env, null, null, cancellationToken);
    }
    
    internal const string ToolName = "pg_dump";
    
    internal static readonly Dictionary<string, string> OptionNames = new()
    {
        { nameof(PgBackupOptions.FileName), "--file" },
        { nameof(PgBackupOptions.Format), "--format" },
        { nameof(PgBackupOptions.Jobs), "--jobs" },
        { nameof(PgBackupOptions.Verbose), "--verbose" },
        { nameof(PgBackupOptions.Compression), "--compress" },
        { nameof(PgBackupOptions.DataOnly), "--data-only" },
        { nameof(PgBackupOptions.IncludeBlobs), "--blobs" },
        { nameof(PgBackupOptions.ExcludeBlobs), "--no-blobs" },
        { nameof(PgBackupOptions.Clean), "--clean" },
        { nameof(PgBackupOptions.Create), "--create" },
        { nameof(PgBackupOptions.Encoding), "--encoding" },
        { nameof(PgBackupOptions.Schema), "--schema" },
        { nameof(PgBackupOptions.ExcludeSchema), "--exclude-schema" },
        { nameof(PgBackupOptions.NoOwner), "--no-owner" },
        { nameof(PgBackupOptions.SchemaOnly), "--schema-only" },
        { nameof(PgBackupOptions.Table), "--table" },
        { nameof(PgBackupOptions.ExcludeTable), "--exclude-table" },
        { nameof(PgBackupOptions.NoPrivileges), "--no-privileges" },
    };
}