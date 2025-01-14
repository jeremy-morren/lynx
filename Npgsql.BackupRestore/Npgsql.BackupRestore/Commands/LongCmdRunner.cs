using System.Diagnostics;

namespace Npgsql.BackupRestore.Commands;

internal static class LongCmdRunner
{
    /// <summary>
    /// Runs a command with arguments, optionally providing standard input and capturing standard output.
    /// </summary>
    public static async Task RunAsync(
        string cmd,
        IEnumerable<string> args,
        Dictionary<string,string?> envVars,
        Stream? stdIn,
        Stream? stdOut,
        CancellationToken stopToken)
    {
        args = args.ToList();
        var psi = new ProcessStartInfo
        {
            FileName = cmd,
            RedirectStandardError = true,
            RedirectStandardOutput = stdOut != null,
            RedirectStandardInput = stdIn != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        foreach (var (key, value) in envVars)
            psi.Environment.Add(key, value);

        using var process = new Process();
        process.StartInfo = psi;
        
        try
        {
            process.EnableRaisingEvents = true;
            
            if (!process.Start())
                throw new Exception($"Failed to start process {cmd}");

            var writeStdInTask = stdIn != null ? WriteStdIn(process, stdIn, stopToken) : Task.CompletedTask;
            var readStdOutTask = stdOut != null
                ? process.StandardOutput.BaseStream.CopyToAsync(stdOut, stopToken)
                : Task.CompletedTask;
            var readErrorTask = process.StandardError.ReadToEndAsync(stopToken);
            
            // Wait for the process to exit, and for the streams to be fully read
            // Don't wait on stdin, because if the command fails then a stream write exception is thrown
            // Instead, wait for the process to exit then throw if it failed
            await Task.WhenAll(process.WaitForExitAsync(stopToken), readStdOutTask, readErrorTask);

            if (process.ExitCode != 0)
            {
                var stdErr = readErrorTask.Result;
                throw new PgToolCommandFailedException(cmd, args.ToList(), process.ExitCode, stdErr);
            }
            // If we succeeded, wait on the (now certainly completed) stdin write task
            await writeStdInTask;
        }
        catch (OperationCanceledException)
        {
            // Cancelled, kill process
            process.Kill();
            throw;
        }
    }

    private static async Task WriteStdIn(Process process, Stream stdIn, CancellationToken ct)
    {
        await stdIn.CopyToAsync(process.StandardInput.BaseStream, ct);
        await process.StandardInput.BaseStream.FlushAsync(ct);
        process.StandardInput.BaseStream.Close();
    }
}