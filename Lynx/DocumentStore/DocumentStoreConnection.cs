using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.DocumentStore;

/// <summary>
/// A document store connection.
/// </summary>
/// <remarks>
/// Closes the connection if we opened it.
/// </remarks>
internal sealed class DocumentStoreConnection : IDisposable, IAsyncDisposable
{
    private readonly DbConnection? _connection;
    private readonly DbContext _context;

    private DocumentStoreConnection(DbConnection? connection, DbContext context)
    {
        _connection = connection;
        _context = context;
    }

    //NB: We don't need to close the connection if we didn't open it

    public void Dispose()
    {
        _connection?.Close();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
            await _connection.CloseAsync();
    }

    public IDbContextTransaction? BeginTransaction()
    {
        if (_context.Database.CurrentTransaction != null)
            return null; //A transaction was started above us, so we don't need to start one
        return _context.Database.BeginTransaction();
    }

    public Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct)
    {
        if (_context.Database.CurrentTransaction != null)
            //A transaction was started above us, so we don't need to start one
            return Task.FromResult<IDbContextTransaction?>(null);

        return _context.Database.BeginTransactionAsync(ct)!;
    }

    public static DocumentStoreConnection OpenConnection(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State == ConnectionState.Open)
            return new DocumentStoreConnection(null, context);

        connection.Open();
        //NB: We opened the connection, so we need to close it
        return new DocumentStoreConnection(connection, context);
    }

    public static async Task<DocumentStoreConnection> OpenConnectionAsync(DbContext context, CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State == ConnectionState.Open)
            return new DocumentStoreConnection(null, context);

        await connection.OpenAsync(ct);
        //NB: We opened the connection, so we need to close it
        return new DocumentStoreConnection(connection, context);
    }
}