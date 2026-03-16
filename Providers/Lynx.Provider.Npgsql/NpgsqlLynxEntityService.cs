using System.Data.Common;
using System.Diagnostics;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Npgsql;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlLynxEntityService<T> : ILynxEntityServiceBulk<T>
    where T : class
{
    public NpgsqlLynxEntityService(RootEntityInfo<T> entity)
    {
        _addParameters = AddParameterDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder>.Build(entity);
        _setParameterValues = SetParameterValueDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder, T>.Build(entity);

        _columns = NpgsqlEntityColumnBuilder<T, NpgsqlBinaryImporter>.GetColumnInfo(entity);

        var generator = new NpgsqlCommandGenerator(entity);
        _insertWithKeyCommand = generator.GetInsertWithKeyCommand();
        _upsertCommand = generator.GetUpsertCommand();

        _insertBinaryCommand = generator.GenerateBinaryCopyInsertCommand();
        _createTempTableCommand = generator.GetCreateTempTableCommand();
        _insertTempTableBinaryCommand = generator.GenerateBinaryCopyTempTableInsertCommand();
        _upsertTempTableCommand = generator.GenerateUpsertTempTableCommand();
        _dropTempTableCommand = generator.GetDropTempTableCommand();
    }

    private static NpgsqlConnection GetNpgsqlConnection(DbTransaction transaction)
    {
        Debug.Assert(transaction.Connection is NpgsqlConnection, "connection must be NpgsqlConnection");
        return (NpgsqlConnection)transaction.Connection;
    }

    #region Single

    /// <summary>
    /// Action to add parameters to a command
    /// </summary>
    private readonly Action<NpgsqlCommand> _addParameters;

    /// <summary>
    /// Action to set parameter values for an entity
    /// </summary>
    private readonly Action<NpgsqlCommand, T> _setParameterValues;

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
        string commandText,
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(transaction is NpgsqlTransaction);
        var npgsqlConnection = GetNpgsqlConnection(transaction);
        
        using var command = npgsqlConnection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)transaction;
        
        command.CommandText = commandText;
        _addParameters(command);
        command.Prepare();

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
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
        Debug.Assert(transaction is NpgsqlTransaction);
        var npgsqlConnection = GetNpgsqlConnection(transaction);

        await using var command = npgsqlConnection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)transaction;
        
        command.CommandText = commandText;
        _addParameters(command);
        await command.PrepareAsync(cancellationToken); //Prepare command with parameters

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
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
        Debug.Assert(transaction is NpgsqlTransaction);
        var npgsqlConnection = GetNpgsqlConnection(transaction);

        await using var command = npgsqlConnection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)transaction;

        command.CommandText = commandText;
        _addParameters(command);
        await command.PrepareAsync(cancellationToken); //Prepare command with parameters

        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            if (entity == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));

            cancellationToken.ThrowIfCancellationRequested();
            _setParameterValues(command, entity);
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
        ExecuteNonQueryAsync(_insertWithKeyCommand, entities,transaction, cancellationToken);

    public Task UpsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(_upsertCommand, entities, transaction, cancellationToken);

    #endregion

    #region Bulk

    private readonly NpgsqlEntityColumn<T, NpgsqlBinaryImporter>[] _columns;

    /// <summary>
    /// Command to insert rows to table in binary format
    /// </summary>
    private readonly string _insertBinaryCommand;

    /// <summary>
    /// Command to create a temporary table
    /// </summary>
    private readonly string _createTempTableCommand;

    /// <summary>
    /// Command to insert rows to temporary table in binary format
    /// </summary>
    private readonly string _insertTempTableBinaryCommand;

    /// <summary>
    /// Command to upsert rows from temporary table to main table
    /// </summary>
    private readonly string _upsertTempTableCommand;

    /// <summary>
    /// Command to drop temporary table
    /// </summary>
    private readonly string _dropTempTableCommand;

    public void BulkInsert(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var npgsqlConnection = GetNpgsqlConnection(transaction);
        using var writer = npgsqlConnection.BeginBinaryImport(_insertBinaryCommand);
        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));

            cancellationToken.ThrowIfCancellationRequested();
            writer.StartRow();
            foreach (var column in _columns)
                column.Write(entity, writer);
        }
        writer.Complete();
    }

    public void BulkUpsert(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var npgsqlConnection = GetNpgsqlConnection(transaction);

        using var command = npgsqlConnection.CreateCommand();

        //Create temp table
        command.CommandText = _createTempTableCommand;
        command.ExecuteNonQuery();

        cancellationToken.ThrowIfCancellationRequested();

        //Insert into temp table
        using (var writer = npgsqlConnection.BeginBinaryImport(_insertTempTableBinaryCommand))
        {
            foreach (var entity in entities)
            {
                if (entity == null)
                    throw new ArgumentException("Entity cannot be null", nameof(entities));

                cancellationToken.ThrowIfCancellationRequested();
                writer.StartRow();
                foreach (var column in _columns)
                    column.Write(entity, writer);
            }
            writer.Complete();
        }

        //Upsert from temp table
        command.CommandText = _upsertTempTableCommand;
        cancellationToken.ThrowIfCancellationRequested();
        command.ExecuteNonQuery();

        //Drop temp table
        command.CommandText = _dropTempTableCommand;
        cancellationToken.ThrowIfCancellationRequested();
        command.ExecuteNonQuery();
    }

    public async Task BulkInsertAsync(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var npgsqlConnection = GetNpgsqlConnection(transaction);
        await using var writer = await npgsqlConnection.BeginBinaryImportAsync(_insertBinaryCommand, cancellationToken);
        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));

            await writer.StartRowAsync(cancellationToken);
            foreach (var column in _columns)
                await column.WriteAsync(entity, writer, cancellationToken);
        }
        await writer.CompleteAsync(cancellationToken);
    }

    public async Task BulkUpsertAsync(
        IEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var npgsqlConnection = GetNpgsqlConnection(transaction);

        await using var command = npgsqlConnection.CreateCommand();

        //Create temp table
        command.CommandText = _createTempTableCommand;
        await command.ExecuteNonQueryAsync(cancellationToken);

        //Insert into temp table
        await using (var writer = await npgsqlConnection.BeginBinaryImportAsync(_insertTempTableBinaryCommand, cancellationToken))
        {
            foreach (var v in entities)
            {
                if (v == null)
                    throw new ArgumentException("Entity cannot be null", nameof(entities));
                await writer.StartRowAsync(cancellationToken);
                foreach (var column in _columns)
                    await column.WriteAsync(v, writer, cancellationToken);
            }
            await writer.CompleteAsync(cancellationToken);
        }

        //Upsert from temp table
        command.CommandText = _upsertTempTableCommand;
        await command.ExecuteNonQueryAsync(cancellationToken);

        //Drop temp table
        command.CommandText = _dropTempTableCommand;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task BulkInsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var npgsqlConnection = GetNpgsqlConnection(transaction);

        await using var writer = await npgsqlConnection.BeginBinaryImportAsync(_insertBinaryCommand, cancellationToken);
        await foreach (var entity in entities.WithCancellation(cancellationToken))
        {
            if (entity == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));

            await writer.StartRowAsync(cancellationToken);
            foreach (var column in _columns)
                await column.WriteAsync(entity, writer, cancellationToken);
        }
        await writer.CompleteAsync(cancellationToken);
    }

    public async Task BulkUpsertAsync(
        IAsyncEnumerable<T> entities,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var npgsqlConnection = GetNpgsqlConnection(transaction);

        await using var command = npgsqlConnection.CreateCommand();

        //Create temp table
        command.CommandText = _createTempTableCommand;
        await command.ExecuteNonQueryAsync(cancellationToken);

        //Insert into temp table
        await using (var writer = await npgsqlConnection.BeginBinaryImportAsync(_insertTempTableBinaryCommand, cancellationToken))
        {
            await foreach (var v in entities.WithCancellation(cancellationToken))
            {
                if (v == null)
                    throw new ArgumentException("Entity cannot be null", nameof(entities));
                await writer.StartRowAsync(cancellationToken);
                foreach (var column in _columns)
                    await column.WriteAsync(v, writer, cancellationToken);
            }
            await writer.CompleteAsync(cancellationToken);
        }

        //Upsert from temp table
        command.CommandText = _upsertTempTableCommand;
        await command.ExecuteNonQueryAsync(cancellationToken);

        //Drop temp table
        command.CommandText = _dropTempTableCommand;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    #endregion
}