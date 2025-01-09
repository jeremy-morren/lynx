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
    
    protected static void DeleteFile(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (FileNotFoundException)
        {
            // ignored
        }
    }
}