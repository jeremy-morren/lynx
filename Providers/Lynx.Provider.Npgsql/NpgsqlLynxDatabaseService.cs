using System.Data.Common;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Models;
using Lynx.Provider.Common.Reflection;
using Npgsql;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlLynxDatabaseService<T> : ILynxDatabaseService<T>
    where T : class
{
    public NpgsqlLynxDatabaseService(RootEntityInfo entity)
    {
        if (entity.Type.ClrType != typeof(T))
            throw new ArgumentException("Entity type mismatch", nameof(entity));

        _addParameters = AddParameterDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder>.Build(entity);
        _setParameterValues = SetParameterValueDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder, T>.Build(entity);

        _insertWithKeyCommand = CommandGenerator.GetInsertWithKeyCommand(entity);
        _upsertCommand = CommandGenerator.GetUpsertCommand(entity);
    }

    /// <summary>
    /// Action to add parameters to a command
    /// </summary>
    private readonly Action<NpgsqlCommand> _addParameters;

    /// <summary>
    /// Action to set parameter values for an entity
    /// </summary>
    private readonly Action<NpgsqlCommand, T> _setParameterValues;

    #region Single

    /// <summary>
    /// SQL to insert an entity with a key
    /// </summary>
    private readonly string _insertWithKeyCommand;

    /// <summary>
    /// SQL to upsert an entity
    /// </summary>
    private readonly string _upsertCommand;

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

        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection", nameof(connection));

        using var command = npgsqlConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        foreach (var v in values)
        {
            if (v == null)
                throw new ArgumentException("Entity cannot be null", nameof(values));

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

        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection", nameof(connection));

        await using var command = npgsqlConnection.CreateCommand();
        command.CommandText = commandText;
        _addParameters(command);

        foreach (var v in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, v);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public void Insert(
        DbConnection connection,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(connection, _insertWithKeyCommand, values, cancellationToken);

    public void Upsert(
        DbConnection connection,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQuery(connection, _upsertCommand, values, cancellationToken);

    public async Task InsertAsync(
        DbConnection connection,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default) =>
        await ExecuteNonQueryAsync(connection, _insertWithKeyCommand, values, cancellationToken);

    public async Task UpsertAsync(
        DbConnection connection,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default) =>
        await ExecuteNonQueryAsync(connection, _upsertCommand, values, cancellationToken);

    #endregion

    #region Bulk

    public void InsertBulk(
        DbConnection connection,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(values);

        using var _ = OpenConnection.Open(connection);

        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection", nameof(connection));

        //We use a command to store row values
        using var command = npgsqlConnection.CreateCommand();
        _addParameters(command);

        using var writer = npgsqlConnection.BeginBinaryImport(_insertWithKeyCommand);

        foreach (var v in values)
        {
            if (v == null)
                throw new ArgumentException("Entity cannot be null", nameof(values));

            cancellationToken.ThrowIfCancellationRequested();

            _setParameterValues(command, v);
            writer.StartRow();

            foreach (NpgsqlParameter p in command.Parameters)
                writer.Write(p.Value, p.NpgsqlDbType);
        }
    }

    #endregion
}