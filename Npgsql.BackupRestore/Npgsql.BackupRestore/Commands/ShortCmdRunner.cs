using System.Diagnostics;

namespace Npgsql.BackupRestore.Commands;

internal static class ShortCmdRunner
{
    /// <summary>
    /// Runs a command to completion and returns the output.
    /// </summary>
    public static string Run(string cmd, params string[] args) =>
        Run(cmd, args, new Dictionary<string, string?>(), TimeSpan.FromMinutes(1));

    /// <summary>
    /// Runs a command to completion and returns the output.
    /// </summary>
    public static string Run(string cmd, string[] args, IDictionary<string, string?> envVariables, TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeout.Ticks, nameof(timeout));
        var psi = new ProcessStartInfo
        {
            FileName = cmd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        foreach (var (k, v) in envVariables)
            psi.Environment.Add(k, v);

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException($"Failed to start process {cmd}");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(TimeSpan.FromMinutes(1)))
        {
            //Timed out
            process.Kill();
            throw new InvalidOperationException($"Process timed out. {cmd}");
        }

        if (process.ExitCode != 0)
            throw new PgToolCommandFailedException(cmd, args, process.ExitCode, error);

        return output;
    }
}