// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Npgsql.BackupRestore;

/// <summary>
/// Finds the PostgreSQL tools on the system.
/// </summary>
public static partial class PgToolFinder
{
    /// <summary>
    /// Finds all instances of the PostgreSQL tool with the specified name.
    /// </summary>
    public static IReadOnlyList<string> FindPgTool(string toolName)
    {
        var filename = OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName;
        var result = new List<string>();
        foreach (var directory in GetSearchDirectories())
        {
            try
            {
                result.AddRange(Directory.EnumerateFiles(directory, filename, SearchOption.AllDirectories));
            }
            catch (DirectoryNotFoundException)
            {
                // Skip directories that don't exist
            }
        }

        if (result.Count > 0)
            return result;

        throw new InvalidOperationException($"Could not find the PostgreSQL tool '{toolName}' on the system.");
    }

    /// <summary>
    /// Finds the latest version of the PostgreSQL tool with the specified name.
    /// </summary>
    /// <param name="toolName">Postgres tool name</param>
    /// <param name="version">Required version (matches major version, requires minor version equal or later)</param>
    /// <returns></returns>
    public static string FindPgTool(string toolName, Version version)
    {
        var files = (
                from file in FindPgTool(toolName)
                let v = GetToolVersion(file)
                orderby v descending
                select new
                {
                    Version = v,
                    File = file,
                })
            .ToList();
        
        var latest = files.FirstOrDefault(f => f.Version.Major == version.Major && f.Version >= version);
        if (latest != null)
            return latest.File;

        var versions = string.Join(", ", files.Select(f => f.Version.ToString()));
        throw new InvalidOperationException($"Could not find the PostgreSQL tool {toolName} with version = {version.Major}.* and version >= {version}. Found versions: {versions}");
    }
    
    /// <summary>
    /// Gets directory paths to search for the tools.
    /// </summary>
    private static IEnumerable<string> GetSearchDirectories()
    {
        if (OperatingSystem.IsLinux())
        {
            // On linux, search the default bin paths and postgres installation directory
            return
            [
                "/usr/bin",
                "/usr/local/bin",
                "/usr/lib/postgresql"
            ];
        }

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();
        
        // Get installation directories from Program Files
        var directories = new[]
        {

            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PostgreSQL"),
        };
        // Only search the bin directories
        return directories
            .SelectMany(d =>
            {
                try
                {
                    return Directory.EnumerateDirectories(d, "bin", SearchOption.AllDirectories);
                }
                catch (DirectoryNotFoundException)
                {
                    // Ignore
                    return [];
                }
            });

    }
    
    public static Version GetToolVersion(string tool)
    {
        var version = ShortCmdRunner.Run(tool, "--version").TrimEnd();

        var match = PgToolVersionRegex().Match(version);
        if (match.Success && Version.TryParse(match.Value, out var v))
            return v;
        throw new InvalidOperationException($"Invalid --version output '{version}' for tool '{tool}'");
    }
    
    [GeneratedRegex(@"(?<=^\w+ \(PostgreSQL\) )[\d|\.]+(?= |$)", RegexOptions.CultureInvariant)]
    public static partial Regex PgToolVersionRegex();
}