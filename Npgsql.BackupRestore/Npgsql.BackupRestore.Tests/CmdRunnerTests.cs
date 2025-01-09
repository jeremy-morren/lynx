using System.IO.Compression;
using System.Text;
using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore.Tests;

public class CmdRunnerTests
{
    [Fact]
    public void RunShortCommand()
    {
        var result = ShortCmdRunner.Run(Gzip, "--version");
        result.ShouldStartWith("gzip");
    }
    
    [Fact]
    public async Task RunLongCommand()
    {
        using var ms = new MemoryStream();
        await LongCmdRunner.RunAsync(Gzip, ["--version"], [], null, ms, default);

        var output = Encoding.UTF8.GetString(ms.ToArray());
        output.ShouldBe(ShortCmdRunner.Run(Gzip, "--version"));
    }
    
    [Fact]
    public async Task RunLongCommandWithInput()
    {
        var ct = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
        var input = Guid.NewGuid().ToByteArray();
        using var stdIn = new MemoryStream(input);
        using var stdOut = new MemoryStream();
        await LongCmdRunner.RunAsync(Gzip, ["--stdout"], [], stdIn, stdOut, ct);
        DecompressGzip(stdOut.ToArray()).ShouldBeEquivalentTo(input);
    }
    
    [Fact]
    public void FailShortCommand()
    {
        var ex = Assert.Throws<PgToolCommandFailedException>(() => ShortCmdRunner.Run(Gzip, "--non-existing-option"));
        ex.Command.ShouldBe(Gzip);
        ex.Args.Should().BeEquivalentTo("--non-existing-option");
        ex.ExitCode.ShouldNotBe(0);
        ex.StdError.ShouldNotBeNullOrEmpty();
        ex.Message.ShouldContain(ex.StdError);
    }

    [Fact]
    public async Task FailLongCommand()
    {
        using var ms = new MemoryStream();
        var ex = await Assert.ThrowsAsync<PgToolCommandFailedException>(() =>
            LongCmdRunner.RunAsync(Gzip, ["--non-existing-option"], [], null, ms, default));
        
        ex.Command.ShouldBe(Gzip);
        ex.Args.Should().BeEquivalentTo("--non-existing-option");
        ex.ExitCode.ShouldNotBe(0);
        ex.StdError.ShouldNotBeNullOrEmpty();
        ex.Message.ShouldContain(ex.StdError);
        
        var shortEx = Assert.Throws<PgToolCommandFailedException>(() => ShortCmdRunner.Run(Gzip, "--non-existing-option"));
        ex.Message.ShouldBe(shortEx.Message);
    }

    
    private static string Gzip
    {
        get
        {
            if (OperatingSystem.IsWindows())
                //Use gzip.exe from git for Windows
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Git", "usr", "bin", "gzip.exe");

            return Path.Combine("usr", "bin", "gzip");
        }
    }

    private static byte[] DecompressGzip(byte[] input)
    {
        using var ms = new MemoryStream(input);
        using var gzip = new GZipStream(ms, CompressionMode.Decompress);
        
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}