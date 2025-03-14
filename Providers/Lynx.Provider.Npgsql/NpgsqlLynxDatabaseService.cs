using System.Data.Common;
using Lynx.Providers.Common;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Npgsql;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlLynxDatabaseService<T> : ILynxDatabaseServiceBulk<T>
    where T : class
{
    public NpgsqlLynxDatabaseService(RootEntityInfo entity)
    {
        if (entity.Type.ClrType != typeof(T))
            throw new ArgumentException("Entity type mismatch", nameof(entity));

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
        DbConnection connection,
        string commandText,
        IEnumerable<T> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(commandText);

        using var _ = OpenConnection.Open(connection);
        var npgsqlConnection = ConvertOrThrow(connection);
        using var command = npgsqlConnection.CreateCommand();

        command.CommandText = commandText;
        _addParameters(command);
        command.Prepare();

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
        var npgsqlConnection = ConvertOrThrow(connection);
        await using var command = npgsqlConnection.CreateCommand();

        command.CommandText = commandText;
        _addParameters(command);
        await command.PrepareAsync(cancellationToken); //Prepare command with parameters

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

    public void BulkInsert(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        using var _ = OpenConnection.Open(connection);
        var npgsqlConnection = ConvertOrThrow(connection);
        using var writer = npgsqlConnection.BeginBinaryImport(_insertBinaryCommand);
        foreach (var v in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (v == null)
                throw new ArgumentException("Entity cannot be null", nameof(entities));
            writer.StartRow();
            foreach (var column in _columns)
                column.Write(v, writer);
        }
        writer.Complete();
    }

    public void BulkUpsert(IEnumerable<T> entities, DbConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        using var _ = OpenConnection.Open(connection);
        var npgsqlConnection = ConvertOrThrow(connection);

        using var command = npgsqlConnection.CreateCommand();

        //Create temp table
        command.CommandText = _createTempTableCommand;
        command.ExecuteNonQuery();

        cancellationToken.ThrowIfCancellationRequested();

        //Insert into temp table
        using (var writer = npgsqlConnection.BeginBinaryImport(_insertTempTableBinaryCommand))
        {
            foreach (var v in entities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (v == null)
                    throw new ArgumentException("Entity cannot be null", nameof(entities));
                writer.StartRow();
                foreach (var column in _columns)
                    column.Write(v, writer);
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

    public async Task BulkInsertAsync(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);
        var npgsqlConnection = ConvertOrThrow(connection);

        await using var writer = await npgsqlConnection.BeginBinaryImportAsync(_insertBinaryCommand, cancellationToken);
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

    public async Task BulkUpsertAsync(IEnumerable<T> entities,
        DbConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        await using var _ = await OpenConnection.OpenAsync(connection, cancellationToken);
        var npgsqlConnection = ConvertOrThrow(connection);

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

    #endregion

    private static NpgsqlConnection ConvertOrThrow(DbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection", nameof(connection));
        return npgsqlConnection;
    }
}