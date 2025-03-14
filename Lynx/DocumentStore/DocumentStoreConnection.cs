using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Lynx.DocumentStore;

/// <summary>
/// A document store connection.
/// </summary>
/// <remarks>
/// Closes the connection if we opened it.
/// </remarks>
internal sealed class DocumentStoreConnection : IDisposable, IAsyncDisposable
{
    private readonly DbConnection _connection;
    private readonly bool _closeConnection;

    private DocumentStoreConnection(DbConnection connection, bool closeConnection)
    {
        _connection = connection;
        _closeConnection = closeConnection;
    }
    
    public static implicit operator DbConnection(DocumentStoreConnection connection) => connection._connection;

    //NB: We don't need to close the connection if we didn't open it

    public void Dispose()
    {
        if (_closeConnection)
            _connection.Close();
    }

    public async ValueTask DisposeAsync()
    {
        if (_closeConnection)
            await _connection.CloseAsync();
    }

    public DbTransaction BeginTransaction()
    {
        return _connection.BeginTransaction();
    }

    public ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        return _connection.BeginTransactionAsync(ct)!;
    }

    public static DocumentStoreConnection OpenConnection(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State == ConnectionState.Open)
            return new DocumentStoreConnection(connection, false);

        connection.Open();
        //NB: We opened the connection, so we need to close it
        return new DocumentStoreConnection(connection, true);
    }

    public static async Task<DocumentStoreConnection> OpenConnectionAsync(DbContext context, CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State == ConnectionState.Open)
            return new DocumentStoreConnection(connection, false);

        await connection.OpenAsync(ct);
        //NB: We opened the connection, so we need to close it
        return new DocumentStoreConnection(connection, true);
    }
}