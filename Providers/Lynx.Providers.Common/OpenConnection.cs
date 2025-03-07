using System.Data;
using System.Data.Common;

namespace Lynx.Providers.Common;

/// <summary>
/// Wrapping class that ensures a connection is open and closes it when disposed.
/// </summary>
internal sealed class OpenConnection : IDisposable, IAsyncDisposable
{
    private readonly DbConnection _connection;

    private OpenConnection(DbConnection connection)
    {
        _connection = connection;
    }

    public void Dispose()
    {
        _connection.Close();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
    }

    /// <summary>
    /// Opens a connection if it is not already open.
    /// </summary>
    public static OpenConnection? Open(DbConnection? connection)
    {
        if (connection == null) return null;
        
        if (connection.State == ConnectionState.Open)
            return null; //Connection is already open, nothing to do

        connection.Open();
        return new OpenConnection(connection);
    }

    public static async Task<OpenConnection?> OpenAsync(DbConnection? connection, CancellationToken cancellationToken)
    {
        if (connection == null) return null;
        
        if (connection.State == ConnectionState.Open)
            return null; //Connection is already open, nothing to do

        await connection.OpenAsync(cancellationToken);
        return new OpenConnection(connection);
    }
}