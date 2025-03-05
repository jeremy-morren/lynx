using System.Data;
using JetBrains.Annotations;
using Npgsql.BackupRestore.DataDump;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace Npgsql.BackupRestore;

/// <summary>
/// Dump a PostgreSQL database to a stream (data-only)
/// </summary>
/// <remarks>
/// Uses a C# implementation of the pg_dump tool and the COPY command to dump the data.
/// Data is written as a series of segments, with the length delimited by a 16-bit integer.
/// Format: <c>Schema|Table|Data</c>
/// </remarks>
[PublicAPI]
public static class PgDataDump
{
    public const int DefaultBufferSize = 8192;

    #region Backup

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connection">Connection to PostgreSQL database</param>
    /// <param name="output">Output stream</param>
    /// <param name="segmentSize">Data segment size</param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(
        NpgsqlConnection connection,
        Stream output,
        short segmentSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentSize);

        EnsureDatabase(connection);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        // Ensure backup is read from a consistent snapshot
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var tables = await PgTableReader.GetTablesAsync(connection, cancellationToken);
        var writer = new DataDumpWriter(output);

        await using var cmd = connection.CreateCommand();
        foreach (var t in tables)
        {
            if (await t.IsEmptyAsync(cmd, cancellationToken))
                continue; // Skip empty tables

            // Will be used to restore the table
            await writer.WriteStringAsync(t.ToJson(), cancellationToken);

            await using var data = await connection.BeginRawBinaryCopyAsync(
                t.GetCopyStdOutCommand(),
                cancellationToken);
            await writer.CopyFromStreamAsync(data, segmentSize, cancellationToken);
        }
    }

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connection">Connection to PostgreSQL database</param>
    /// <param name="output">Output stream</param>
    /// <param name="segmentSize">Data segment size</param>
    public static void Backup(
        NpgsqlConnection connection,
        Stream output,
        short segmentSize)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentSize);

        EnsureDatabase(connection);

        if (connection.State != ConnectionState.Open)
            connection.Open();

        // Ensure backup is read from a consistent snapshot
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        var tables = PgTableReader.GetTables(connection);
        var writer = new DataDumpWriter(output);

        using var cmd = connection.CreateCommand();
        foreach (var t in tables)
        {
            if (t.IsEmpty(cmd))
                continue; // Skip empty tables

            writer.WriteString(t.ToJson());
            using var data = connection.BeginRawBinaryCopy(t.GetCopyStdOutCommand());
            writer.CopyFromStream(data, segmentSize);
        }
    }

    #endregion

    #region Restore

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connection">PostgreSQL database connection</param>
    /// <param name="input">Input stream</param>
    /// <param name="bufferSize">Read buffer size</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidDataException"></exception>
    public static async Task RestoreAsync(
        NpgsqlConnection connection,
        Stream input,
        int bufferSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        EnsureDatabase(connection);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var reader = new DataDumpReader(input);
        while (true)
        {
            var table = PgTable.FromJson(await reader.ReadStringAsync(cancellationToken));
            if (table == null)
                break;
            await using var writer = await connection.BeginRawBinaryCopyAsync(table.GetCopyStdInCommand(), cancellationToken);
            var stream = reader.CreateStream(bufferSize);
            await stream.CopyToAsync(writer, cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connection">PostgreSQL database connection</param>
    /// <param name="input">Input stream</param>
    /// <param name="bufferSize">Read buffer size</param>
    public static void Restore(NpgsqlConnection connection, Stream input, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        EnsureDatabase(connection);

        if (connection.State != ConnectionState.Open)
            connection.Open();

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var reader = new DataDumpReader(input);
        while (true)
        {
            var table = PgTable.FromJson(reader.ReadString());
            if (table == null)
                break;
            using var writer = connection.BeginRawBinaryCopy(table.GetCopyStdInCommand());
            var stream = reader.CreateStream(bufferSize);
            stream.CopyTo(writer);
        }
        transaction.Commit();
    }

    #endregion

    #region Overloads

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connection">Connection to PostgreSQL database</param>
    /// <param name="output">Output stream</param>
    /// <param name="cancellationToken"></param>
    public static Task BackupAsync(
        NpgsqlConnection connection,
        Stream output,
        CancellationToken cancellationToken = default) =>
        BackupAsync(connection, output, DefaultBufferSize, cancellationToken);

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="output">Output stream</param>
    /// <param name="segmentSize">Data segment size</param>
    /// <param name="cancellationToken"></param>
    public static async Task BackupAsync(string connectionString,
        Stream output,
        short segmentSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        await using var connection = new NpgsqlConnection(connectionString);
        await BackupAsync(connection, output, segmentSize, cancellationToken);
    }

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="output">Output stream</param>
    /// <param name="cancellationToken"></param>
    public static Task BackupAsync(string connectionString,
        Stream output,
        CancellationToken cancellationToken = default) =>
        BackupAsync(connectionString, output, DefaultBufferSize, cancellationToken);

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="output">Output stream</param>
    /// <param name="segmentSize">Data segment size</param>
    public static void Backup(string connectionString, Stream output, short segmentSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        using var connection = new NpgsqlConnection(connectionString);
        Backup(connection, output, segmentSize);
    }

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connection">Connection to PostgreSQL database</param>
    /// <param name="output">Output stream</param>
    public static void Backup(NpgsqlConnection connection, Stream output) =>
        Backup(connection, output, DefaultBufferSize);

    /// <summary>
    /// Backup a PostgreSQL database to a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="output">Output stream</param>
    public static void Backup(string connectionString, Stream output) =>
        Backup(connectionString, output, DefaultBufferSize);

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL database connection string</param>
    /// <param name="output">Output stream</param>
    /// <param name="bufferSize">Read buffer size</param>
    /// <param name="cancellationToken"></param>
    public static async Task RestoreAsync(
        string connectionString,
        Stream output,
        int bufferSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        await using var connection = new NpgsqlConnection(connectionString);
        await RestoreAsync(connection, output, bufferSize, cancellationToken);
    }

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL database connection string</param>
    /// <param name="output">Output stream</param>
    /// <param name="cancellationToken"></param>
    public static Task RestoreAsync(
        string connectionString,
        Stream output,
        CancellationToken cancellationToken = default) =>
        RestoreAsync(connectionString, output, DefaultBufferSize, cancellationToken);

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connection">PostgreSQL database connection</param>
    /// <param name="output">Output stream</param>
    /// <param name="cancellationToken"></param>
    public static Task RestoreAsync(
        NpgsqlConnection connection,
        Stream output,
        CancellationToken cancellationToken = default) =>
        RestoreAsync(connection, output, DefaultBufferSize, cancellationToken);

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL database connection string</param>
    /// <param name="output">Output stream</param>
    /// <param name="bufferSize">Read buffer size</param>
    public static void Restore(
        string connectionString,
        Stream output,
        int bufferSize)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        using var connection = new NpgsqlConnection(connectionString);
        Restore(connection, output, bufferSize);
    }

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connectionString">PostgreSQL database connection string</param>
    /// <param name="output">Output stream</param>
    public static void Restore(
        string connectionString,
        Stream output) =>
        Restore(connectionString, output, DefaultBufferSize);

    /// <summary>
    /// Restores a PostgreSQL database from a stream
    /// </summary>
    /// <param name="connection">PostgreSQL database connection</param>
    /// <param name="output">Output stream</param>
    public static void Restore(
        NpgsqlConnection connection,
        Stream output) =>
        Restore(connection, output, DefaultBufferSize);

    #endregion

    /// <summary>
    /// Ensure that the connection specifies a database name
    /// </summary>
    private static void EnsureDatabase(NpgsqlConnection conn)
    {
        var database = new NpgsqlConnectionStringBuilder(conn.ConnectionString).Database;
        if (string.IsNullOrEmpty(database))
            throw new InvalidOperationException("Connection must specify a database name");
    }
}