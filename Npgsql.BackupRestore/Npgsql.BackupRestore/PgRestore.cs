using JetBrains.Annotations;
using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore;

/// <summary>
/// Wrapper around the <c>pg_restore</c> tool
/// </summary>
[PublicAPI]
public static class PgRestore
{
    /// <summary>
    /// Restores a PostgreSQL database from a file (using <c>pg_restore</c>)
    /// </summary>
    /// <param name="connectionString">Connection string</param>
    /// <param name="options">Restore options</param>
    /// <param name="filename">Source filename</param>
    /// <param name="cancellationToken"></param>
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
    
    /// <summary>
    /// Restores a PostgreSQL database from a file (using <c>pg_restore</c>)
    /// </summary>
    /// <param name="connection">Connection</param>
    /// <param name="options">Restore options</param>
    /// <param name="filename">Source filename</param>
    /// <param name="cancellationToken"></param>
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
        var args = CommandHelpers.GetArgs(options, OptionNames).Append(filename);
        var env = CommandHelpers.GetEnvVariables(connection.ConnectionString);
        await LongCmdRunner.RunAsync(tool, args, env, null, null, cancellationToken);
    }

    /// <summary>
    /// Restores a PostgreSQL database from a stream (using <c>pg_restore</c>)
    /// </summary>
    /// <param name="connectionString">Connection string</param>
    /// <param name="options">Restore options</param>
    /// <param name="source">Source stream</param>
    /// <param name="cancellationToken"></param>
    public static async Task RestoreAsync(
        string connectionString,
        PgRestoreOptions options,
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(source);
        
        connectionString = PgBackup.PersistSecurityInfo(connectionString);
        await using var connection = new NpgsqlConnection(connectionString);
        await RestoreAsync(connection, options, source, cancellationToken);
    }

    /// <summary>
    /// Restores a PostgreSQL database from a file (using <c>pg_restore</c>)
    /// </summary>
    /// <param name="connection">Connection</param>
    /// <param name="options">Restore options</param>
    /// <param name="source">Source stream</param>
    /// <param name="cancellationToken"></param>
    public static async Task RestoreAsync(
        NpgsqlConnection connection, 
        PgRestoreOptions options, 
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrEmpty(options.Database))
            // pg_restore will fail if --dbname is not specified
            throw new InvalidOperationException($"{nameof(options.Database)} must be specified when restoring from a stream");
        
        var version = await NpgsqlServerHelpers.GetServerVersion(connection, cancellationToken);
        var tool = PgToolFinder.FindPgTool(ToolName, version);
        var args = CommandHelpers.GetArgs(options, OptionNames);
        var env = CommandHelpers.GetEnvVariables(connection.ConnectionString);
        await LongCmdRunner.RunAsync(tool, args, env, source, null, cancellationToken);
    }
    
    internal const string ToolName = "pg_restore";
    
    internal static readonly Dictionary<string, string> OptionNames = new()
    {
        { nameof(PgRestoreOptions.Database), "--dbname" },
        { nameof(PgRestoreOptions.Format), "--format" },
        { nameof(PgRestoreOptions.DataOnly), "--data-only" },
        { nameof(PgRestoreOptions.Clean), "--clean" },
        { nameof(PgRestoreOptions.Create), "--create" },
        { nameof(PgRestoreOptions.IfExists), "--if-exists"},
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