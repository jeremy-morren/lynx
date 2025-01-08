using System.Diagnostics;

namespace Lynx.NpgsqlBackupRestore;

/// <summary>
/// A runner for long running commands.
/// </summary>
public class LongCmdRunner
{
    public static async Task RunLongCmdAsync(
        string cmd,
        IEnumerable<string> args,
        Stream stdOut,
        Action<string?> onStdErr,
        CancellationToken stopToken)
    {
        args = args.ToList();
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

        using var process = new Process();
        process.StartInfo = psi;
        
        try
        {
            var copyTask = process.StandardOutput.BaseStream.CopyToAsync(stdOut, stopToken);

            process.EnableRaisingEvents = true;
            process.ErrorDataReceived += (_, e) => onStdErr(e.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.Start())
                throw new Exception($"Failed to start process {cmd}");

            await Task.WhenAll(copyTask, process.WaitForExitAsync(stopToken));
        }
        catch (OperationCanceledException)
        {
            // Cancelled, kill process
            process.Kill();
            throw;
        }
    }
}