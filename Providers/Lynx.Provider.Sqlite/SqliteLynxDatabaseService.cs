using System.Data.Common;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.Data.Sqlite;

namespace Lynx.Provider.Sqlite;

internal class SqliteLynxDatabaseService<T> : ILynxDatabaseService<T>
    where T : class
{
    public SqliteLynxDatabaseService(RootEntityInfo entity)
    {
        if (entity.Type.ClrType != typeof(T))
            throw new ArgumentException("Entity type mismatch", nameof(entity));

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
        IEnumerable<T> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        using var _ = OpenConnection.Open(connection);

        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection", nameof(connection));

        using var command = sqliteConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        foreach (var v in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, v);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private async Task ExecuteNonQueryAsync(
        DbConnection connection,
        string commandText,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);

        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection", nameof(connection));

        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        foreach (var v in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, v);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public void Insert(IEnumerable<T> values,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(connection, _insertWithKeyCommand, values, cancellationToken);

    public void Upsert(IEnumerable<T> values,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(connection, _upsertCommand, values, cancellationToken);

    public async Task InsertAsync(IEnumerable<T> values,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        await ExecuteNonQueryAsync(connection, _insertWithKeyCommand, values, cancellationToken);

    public async Task UpsertAsync(IEnumerable<T> values,
        DbConnection connection,
        CancellationToken cancellationToken = default) =>
        await ExecuteNonQueryAsync(connection, _upsertCommand, values, cancellationToken);
}