using System.Text;
using JetBrains.Annotations;

namespace Npgsql.BackupRestore.Commands;

[PublicAPI]
public class PgToolCommandFailedException : Exception
{
    public string Command { get; }
    public IReadOnlyList<string> Args { get; }
    public int ExitCode { get; }
    public string? StdError { get; }

    public PgToolCommandFailedException(string command, IReadOnlyList<string> args, int exitCode, string? stdError)
        : base(FormatMessage(command, args.ToList(), exitCode, stdError))
    {
        Command = command;
        Args = args.ToList();
        ExitCode = exitCode;
        StdError = stdError;
    }
    
    private static string FormatMessage(string command, IReadOnlyList<string> args, int exitCode, string? stdError)
    {
        var sb = new StringBuilder();
        sb.Append($"{command} ");
        if (args.Count > 0)
            sb.Append($"{FormatArgs(args)} ");
        sb.Append($"failed with exit code {exitCode}");
        if (string.IsNullOrEmpty(stdError)) 
            return sb.ToString();
        sb.AppendLine();
        sb.AppendLine(stdError);
        return sb.ToString();
    }

    private static string FormatArgs(IEnumerable<string> args)
    {
        args =
            from a in args
            select a.Contains(' ') || a.Contains('\"')
                ? $"\"{a.Replace("\"", "\\\"")}\""
                : a;
        return string.Join(" ", args);
    }
}