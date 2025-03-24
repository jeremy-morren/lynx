using System.Data.Common;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.Data.Sqlite;

namespace Lynx.Provider.Sqlite;

internal class SqliteLynxEntityService<T> : ILynxEntityService<T>
    where T : class
{
    public SqliteLynxEntityService(RootEntityInfo<T> entity)
    {
        var generator = new CommandGenerator(entity);
        _insertWithKeyCommand = generator.GetInsertWithKeyCommand();
        _upsertCommand = generator.GetUpsertCommand();

        _addParameters = AddParameterDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder>.Build(entity);
        _setParameterValues = SetParameterValueDelegateBuilder<SqliteCommand, SqliteProviderDelegateBuilder, T>.Build(entity);
    }

    /// <summary>
    /// SQL to insert an entity with a key
    /// </summary>
    private readonly string _insertWithKeyCommand;

    /// <summary>
    /// SQL to upsert an entity
    /// </summary>
    private readonly string _upsertCommand;

    /// <summary>
    /// Action to add parameters to a command
    /// </summary>
    private readonly Action<SqliteCommand> _addParameters;

    /// <summary>
    /// Action to set parameter values for an entity
    /// </summary>
    private readonly Action<SqliteCommand, T> _setParameterValues;

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private void ExecuteNonQuery(
        DbConnection connection,
        string commandText,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        using var _ = OpenConnection.Open(connection);
        var sqliteConnection = GetConnectionOrThrow(connection);

        using var command = sqliteConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        command.Prepare();

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), "Entity cannot be null");

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private async Task ExecuteNonQueryAsync(
        DbConnection connection,
        string commandText,
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);
        var sqliteConnection = GetConnectionOrThrow(connection);

        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        await command.PrepareAsync(cancellationToken);

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), "Entity cannot be null");

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private async Task ExecuteNonQueryAsync(
        DbConnection connection,
        string commandText,
        IAsyncEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);
        var sqliteConnection = GetConnectionOrThrow(connection);

        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        await command.PrepareAsync(cancellationToken);

        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), "Entity cannot be null");

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public void Insert(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(connection, _insertWithKeyCommand, entities, cancellationToken);

    public void Upsert(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(connection, _upsertCommand, entities, cancellationToken);

    public Task InsertAsync(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(connection, _insertWithKeyCommand, entities, cancellationToken);

    public Task UpsertAsync(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(connection, _upsertCommand, entities, cancellationToken);

    public Task InsertAsync(IAsyncEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(connection, _insertWithKeyCommand, entities, cancellationToken);

    public Task UpsertAsync(IAsyncEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(connection, _upsertCommand, entities, cancellationToken);

    private static SqliteConnection GetConnectionOrThrow(DbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection", nameof(connection));

        return sqliteConnection;
    }
}