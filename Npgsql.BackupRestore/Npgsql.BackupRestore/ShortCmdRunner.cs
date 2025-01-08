using System.Diagnostics;

namespace Npgsql.BackupRestore;

internal static class ShortCmdRunner
{
    /// <summary>
    /// Runs a command to completion and returns the output.
    /// </summary>
    public static string Run(string cmd, params string[] args)
    {
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
            throw new InvalidOperationException($"Process {cmd} {string.Join(" ", args)} failed. Exit code: {process.ExitCode}. Error: {error}");

        return output;
    }
}