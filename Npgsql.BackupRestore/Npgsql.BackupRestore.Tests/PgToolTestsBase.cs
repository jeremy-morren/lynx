using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore.Tests;

[SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public class PgToolTestsBase
{
    protected const string ConnString = "Host=localhost;Username=postgres;Password=postgres";
    protected const string Database = "postgres";
    protected const string FullConnString = $"{ConnString};Database={Database}";

    /// <summary>
    /// Gets all option names for the given tool, by running --help
    /// </summary>
    protected static List<string> GetOptionNames(string tool)
    {
        var help = ShortCmdRunner.Run(tool, "--help");
        // Extract all actual names from the help, and ensure all option names are present
        var optionNames = Regex.Matches(help, @"(?<=\W)--[\w|-]+(?=[\W|=])").Select(m => m.Value).ToList();
        optionNames.Should().HaveCountGreaterThan(1);
        optionNames.ShouldAllBe(n => n.StartsWith("--"));
        return optionNames;
    }
    
    protected static string GetSchemaWithTables()
    {
        using var conn = new NpgsqlConnection(FullConnString);
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT table_schema, COUNT(1) AS table_count
                          FROM information_schema.tables
                          WHERE table_type = 'BASE TABLE'
                          GROUP BY table_schema
                          ORDER BY table_count
                          LIMIT 1
                          """;
        var result = cmd.ExecuteScalar();
        return result.ShouldBeOfType<string>().ShouldNotBeNull();
    }

    protected static object? ExecuteScalar(string connString, string sql)
    {
        using var conn = new NpgsqlConnection(connString);
        if (conn.State != ConnectionState.Open)
            conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }
    
    protected static void ExecuteNonQuery(string connString, string sql)
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
    protected static void CleanDatabase(string dbName)
    {
        DropDatabase(dbName);
        ExecuteNonQuery(ConnString, $"CREATE DATABASE \"{dbName}\"");
    }
    
    /// <summary>
    /// Drops and recreates the given database
    /// </summary>
    /// <param name="dbName"></param>
    protected static void DropDatabase(string dbName)
    {
        try
        {
            ExecuteNonQuery(ConnString, $"DROP DATABASE \"{dbName}\" WITH (FORCE)");
        }
        catch (NpgsqlException e) when (e.SqlState == "3D000")
        {
            // Database doesn't exist, ignore
        }
    }
    
    protected static void DeleteFileOrDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
            // ignored
        }
        catch (FileNotFoundException)
        {
            // ignored
        }
    }
}