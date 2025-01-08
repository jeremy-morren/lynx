using System.Data;
using Npgsql;

namespace Lynx.NpgsqlBackupRestore;

[PublicAPI]
public static class NpgsqlServerHelpers
{
    
    public static async Task<Version> GetServerVersion(string connectionString, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        return await GetServerVersion(connection, ct);
    }
    
    public 
    
    public static async Task<Version> GetServerVersion(NpgsqlConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
        
        await using var cmd = new NpgsqlCommand("SHOW server_version", connection);
        var version = (string?)await cmd.ExecuteScalarAsync(ct);
        if (!Version.TryParse(version, out var v))
            throw new Exception($"Failed to parse server version: {version}");
        return v;
    }
}