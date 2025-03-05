using System.Data;
using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore.Tests;

/// <summary>
/// A database that is seeded with sample data.
/// </summary>
/// <remarks>
/// See https://raw.githubusercontent.com/neondatabase/postgres-sample-dbs/main/pagila.sql
/// </remarks>
public static class SampleDatabase
{
    public const string MasterConnString = "Host=localhost;Username=postgres;Password=postgres";
    public const string DatabaseName = "pagila";
    public const string ConnString = $"{MasterConnString};Database={DatabaseName}";

    static SampleDatabase()
    {
        // Create the database if it doesn't exist
        try
        {
            ExecuteNonQuery(MasterConnString, $"CREATE DATABASE \"{DatabaseName}\"");
            var filename = DownloadSampleDatabase();
            var psql = PgToolFinder.FindPgTool("psql")[0];
            ShortCmdRunner.Run(psql,
                ["-f", filename],
                CommandHelpers.GetEnvVariables(ConnString),
                TimeSpan.FromMinutes(1));
            File.Delete(filename);
        }
        catch (NpgsqlException e) when (e.SqlState == "42P04")
        {
            // Database already exists, ignore
        }
    }

    public static void EnsureCreated()
    {
        // No-op, just to ensure the static constructor runs
    }

    #region Commands

    public static void ExecuteNonQuery(string connString, string sql)
    {
        using var conn = new NpgsqlConnection(connString);
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Drops and recreates the given database
    /// </summary>
    /// <param name="dbName"></param>
    public static void CleanDatabase(string dbName)
    {
        DropDatabase(dbName);
        ExecuteNonQuery(MasterConnString, $"CREATE DATABASE \"{dbName}\"");
    }

    /// <summary>
    /// Drops and recreates the given database
    /// </summary>
    /// <param name="dbName"></param>
    public static void DropDatabase(string dbName)
    {
        try
        {
            ExecuteNonQuery(MasterConnString, $"DROP DATABASE \"{dbName}\" WITH (FORCE)");
        }
        catch (NpgsqlException e) when (e.SqlState == "3D000")
        {
            // Database doesn't exist, ignore
        }
    }

    public static void DeleteFileOrDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
        else if (File.Exists(path))
            File.Delete(path);
    }

    #endregion

    #region Sample database download

    private const string SampleDatabaseUrl = "https://raw.githubusercontent.com/neondatabase/postgres-sample-dbs/main/pagila.sql";
    private static readonly HttpClient HttpClient = new();

    private static string DownloadSampleDatabase()
    {
        var filename = Path.Combine(Path.GetTempPath(), "pagila.sql");
        if (File.Exists(filename))
            return filename;

        var request = new HttpRequestMessage(HttpMethod.Get, SampleDatabaseUrl);
        using var response = HttpClient.Send(request);
        response.EnsureSuccessStatusCode();
        using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
        response.Content.CopyTo(fs, null, default);
        return filename;
    }

    #endregion
}