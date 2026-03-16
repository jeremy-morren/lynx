using Npgsql.BackupRestore.Commands;

namespace Npgsql.BackupRestore.Tests;

public class CommandRunnerTests
{
    [Fact]
    public void RunShortCommand()
    {
        ShortCmdRunner.Run(GetShellCommand(), CreateEchoCommand("HelloWorld"))
            .ShouldBe($"HelloWorld{Environment.NewLine}");
    }

    [Fact]
    public async Task RunLongCommand()
    {
        var result = await LongCmdRunner.RunAsync(
            GetShellCommand(),
            CreateEchoCommand("HelloWorld"),
            new Dictionary<string, string?>(),
            null,
            default);
        result.ShouldBe($"HelloWorld{Environment.NewLine}");
    }

    private static string GetShellCommand() => OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";

    private static string[] CreateEchoCommand(string result) =>
        OperatingSystem.IsWindows()
            ? ["/Q", "/D", "/C", "echo", result]
            : ["-c", $"echo {result}"];
}