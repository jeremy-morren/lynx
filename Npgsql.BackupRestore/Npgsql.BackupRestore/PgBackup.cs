using JetBrains.Annotations;
using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore;

/// <summary>
/// Methods to backup a PostgreSQL database (using <c>pg_dump</c>)
/// </summary>
[PublicAPI]
public static class PgBackup
{
    /// <summary>
    /// Runs a pg_dump command to backup a PostgreSQL database to a file
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="options"></param>
    /// <param name="database">Database to backup</param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(
        string connectionString,
        PgBackupOptions options,
        string database,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(database);

        connectionString = PersistSecurityInfo(connectionString);
        
        await using var connection = new NpgsqlConnection(connectionString);
        await BackupAsync(connection, options, database, cancellationToken);
    }

    /// <summary>
    /// Runs a pg_dump command to backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="options"></param>
    /// <param name="database">Database to backup</param>
    /// <param name="destination">Destination stream</param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(
        string connectionString,
        PgBackupOptions options,
        string database,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrEmpty(database);
        
        connectionString = PersistSecurityInfo(connectionString);
        
        await using var connection = new NpgsqlConnection(connectionString);
        await BackupAsync(connection, options, database, destination, cancellationToken);
    }

    /// <summary>
    /// Runs a pg_dump command to backup a PostgreSQL database to a file
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="options"></param>
    /// <param name="database">Database to backup</param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(
        NpgsqlConnection connection,
        PgBackupOptions options,
        string database,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(database);
        
        if (options.FileName == null)
            throw new ArgumentException("FileName must be set", nameof(options));
        
        var version = await NpgsqlServerHelpers.GetServerVersion(connection, cancellationToken);
        // Get pg_dump that matches the server version
        var pgDump = PgToolFinder.FindPgTool(ToolName, version);
        var args = CommandHelpers.GetArgs(options, OptionNames).Append(database).ToList();
        var env = CommandHelpers.GetEnvVariables(connection.ConnectionString);
        await LongCmdRunner.RunAsync(pgDump, args, env, null, null, cancellationToken);
    }

    /// <summary>
    /// Runs a pg_dump command to backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="options"></param>
    /// <param name="database">Database to backup</param>
    /// <param name="destination">Destination stream</param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(
        NpgsqlConnection connection,
        PgBackupOptions options,
        string database,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrEmpty(database);
        
        if (options.FileName != null)
            throw new ArgumentException("FileName must be null", nameof(options));
        
        var version = await NpgsqlServerHelpers.GetServerVersion(connection, cancellationToken);
        // Get pg_dump that matches the server version
        var pgDump = PgToolFinder.FindPgTool(ToolName, version);
        
        var args = CommandHelpers.GetArgs(options, OptionNames).Append(database).ToList();
        var env = CommandHelpers.GetEnvVariables(connection.ConnectionString);
        await LongCmdRunner.RunAsync(pgDump, args, env, null, destination, cancellationToken);
    }
    
    internal const string ToolName = "pg_dump";
    
    internal static readonly Dictionary<string, string> OptionNames = new()
    {
        { nameof(PgBackupOptions.FileName), "--file" },
        { nameof(PgBackupOptions.Format), "--format" },
        { nameof(PgBackupOptions.Jobs), "--jobs" },
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
        { nameof(PgBackupOptions.Inserts), "--inserts"},
        { nameof(PgBackupOptions.ColumnInserts), "--column-inserts"},
        { nameof(PgBackupOptions.Verbose), "--verbose" },
    };
    
    internal static string PersistSecurityInfo(string connectionString)
    {
        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            //Ensure we can extract the password from the connection string below
            PersistSecurityInfo = true
        }.ConnectionString;
    }
}