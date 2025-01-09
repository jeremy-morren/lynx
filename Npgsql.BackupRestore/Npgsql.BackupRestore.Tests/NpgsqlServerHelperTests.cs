namespace Npgsql.BackupRestore.Tests;

public class NpgsqlServerHelperTests
{
    [Fact]
    public async Task GetVersion()
    {
        await using var conn = new NpgsqlConnection(ConnString);
        var version = (await NpgsqlServerHelpers.GetServerVersion(ConnString, default)).ShouldNotBeNull();
        version.Major.ShouldBePositive();
        (await NpgsqlServerHelpers.GetServerVersion(ConnString, default)).ShouldBe(version);
        
        var dataSource = new NpgsqlDataSourceBuilder(ConnString).Build();
        (await NpgsqlServerHelpers.GetServerVersion(dataSource, default)).ShouldBe(version);
    }
    
    private const string ConnString = "Host=localhost;Username=postgres;Password=postgres";
}