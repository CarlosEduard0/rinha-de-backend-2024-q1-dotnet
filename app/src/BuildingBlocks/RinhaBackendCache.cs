using StackExchange.Redis;

namespace RinhaBackend;

public static class RinhaBackendCache
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Redis")!;
    public static readonly IDatabase Database = ConnectionMultiplexer.Connect(ConnectionString).GetDatabase();

    public static async Task Load()
    {
        Console.WriteLine("[Cache] Loading...");
        await Database.PingAsync();
        Console.WriteLine("[Cache] Loaded");
    }
}
