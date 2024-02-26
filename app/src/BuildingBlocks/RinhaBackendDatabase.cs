using Npgsql;

namespace RinhaBackend;

public static class RinhaBackendDatabase
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL")!;
    public static readonly NpgsqlDataSource DataSource = new NpgsqlSlimDataSourceBuilder(ConnectionString).Build();

    public static async Task Load()
    {
        Console.WriteLine("[Database] Loading...");
        await using var connection = await DataSource.OpenConnectionAsync();
        var pingCommand = connection.CreateCommand();
        pingCommand.CommandText = "SELECT 1";
        await pingCommand.ExecuteNonQueryAsync();
        Console.WriteLine("[Database] loaded");
    }
}
