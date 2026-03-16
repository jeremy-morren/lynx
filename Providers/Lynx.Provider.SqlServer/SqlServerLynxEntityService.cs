using System.Data.Common;
using System.Diagnostics;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.Data.SqlClient;
// ReSharper disable InvertIf

namespace Lynx.Provider.SqlServer;

internal class SqlServerLynxEntityService<T> : ILynxEntityService<T>
    where T : class
{
    public SqlServerLynxEntityService(RootEntityInfo<T> entity)
    {
        var generator = new SqlServerCommandGenerator(entity);

        _insertWithKeyCommand = generator.GetInsertWithKeyCommand();
        _upsertCommand = generator.GetUpsertCommand();

        _setIdentityInsertOnCommand = generator.GetSetIdentityInsertCommand(true);
        _setIdentityInsertOffCommand = generator.GetSetIdentityInsertCommand(false);

        _addParameters = AddParameterDelegateBuilder<SqlCommand, SqlServerProviderDelegateBuilder>.Build(entity);
        _setParameterValues = SetParameterValueDelegateBuilder<SqlCommand, SqlServerProviderDelegateBuilder, T>.Build(entity);
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
    /// SQL to set identity insert ON, if necessary
    /// </summary>
    private readonly string? _setIdentityInsertOnCommand;

    /// <summary>
    /// SQL to set identity insert OFF, if necessary
    /// </summary>
    private readonly string? _setIdentityInsertOffCommand;

    /// <summary>
    /// Action to add parameters to a command
    /// </summary>
    private readonly Action<SqlCommand> _addParameters;

    /// <summary>
    /// Action to set parameter values for an entity
    /// </summary>
    private readonly Action<SqlCommand, T> _setParameterValues;

    private static SqlConnection GetSqlConnection(DbTransaction transaction)
    {
        Debug.Assert(transaction.Connection is SqlConnection, "connection must be a SqlConnection");
        return (SqlConnection)transaction.Connection;
    }

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private void ExecuteNonQuery(
        string commandText,
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(transaction is SqlTransaction);
        var sqlConnection = GetSqlConnection(transaction);

        using var command = sqlConnection.CreateCommand();
        command.Transaction = (SqlTransaction)transaction;

        if (_setIdentityInsertOnCommand != null)
        {
            command.CommandText = _setIdentityInsertOnCommand;
            command.ExecuteNonQuery();
        }

        command.CommandText = commandText;
        _addParameters(command);

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), "Entity cannot be null");

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
            command.ExecuteNonQuery();
        }

        if (_setIdentityInsertOffCommand != null)
        {
            command.Parameters.Clear();
            command.CommandText = _setIdentityInsertOffCommand;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private async Task ExecuteNonQueryAsync(
        string commandText,
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(transaction is SqlTransaction);
        var sqlConnection = GetSqlConnection(transaction);

        await using var command = sqlConnection.CreateCommand();
        command.Transaction = (SqlTransaction)transaction;

        if (_setIdentityInsertOnCommand != null)
        {
            command.CommandText = _setIdentityInsertOnCommand;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        command.CommandText = commandText;
        _addParameters(command);

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), "Entity cannot be null");

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (_setIdentityInsertOffCommand != null)
        {
            command.Parameters.Clear();
            command.CommandText = _setIdentityInsertOffCommand;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Executes a non-query command for a collection of entities
    /// </summary>
    private async Task ExecuteNonQueryAsync(
        string commandText,
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(transaction is SqlTransaction);
        var sqlConnection = GetSqlConnection(transaction);

        await using var command = sqlConnection.CreateCommand();
        command.Transaction = (SqlTransaction)transaction;

        if (_setIdentityInsertOnCommand != null)
        {
            command.CommandText = _setIdentityInsertOnCommand;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        command.CommandText = commandText;
        _addParameters(command);

        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entities), "Entity cannot be null");

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (_setIdentityInsertOffCommand != null)
        {
            command.Parameters.Clear();
            command.CommandText = _setIdentityInsertOffCommand;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public void Insert(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(_insertWithKeyCommand, entities, transaction, cancellationToken);

    public void Upsert(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(_upsertCommand, entities, transaction, cancellationToken);

    public Task InsertAsync(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(_insertWithKeyCommand, entities, transaction, cancellationToken);

    public Task UpsertAsync(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(_upsertCommand, entities, transaction, cancellationToken);

    public Task InsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(_insertWithKeyCommand, entities, transaction, cancellationToken);

    public Task UpsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(_upsertCommand, entities, transaction, cancellationToken);
}