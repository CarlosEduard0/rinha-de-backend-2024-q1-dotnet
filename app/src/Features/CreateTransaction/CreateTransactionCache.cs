using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using StackExchange.Redis;

namespace RinhaBackend;

public static class CreateTransactionCache
{
    private static readonly BoundedChannelOptions _channelOptions = new(10)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    };
    private static readonly ConcurrentDictionary<int, Channel<CreateTransactionCacheEntry>> _buffers = new();

    public static void Load(CancellationToken cancellationToken)
    {
        Console.WriteLine("[TransactionCache] Loading...");
        _ = Flush(cancellationToken);
        _ = CalculateThroughputBySecond(cancellationToken);
        Console.WriteLine("[TransactionCache] Loaded");
    }

    private static bool shouldUseBuffer = false;
    public static void Write(CreateTransactionCacheEntry entry)
    {
        Interlocked.Increment(ref CacheWriteThroughput);
        if (shouldUseBuffer)
        {
            var channel = _buffers.GetOrAdd(entry.ClientId, id => Channel.CreateBounded<CreateTransactionCacheEntry>(_channelOptions));
            channel.Writer.TryWrite(entry);

            return;
        }
        
        var key = $"user:{entry.ClientId}:last_transactions";
        var cacheEntry = JsonSerializer.Serialize(entry, AppJsonSerializerContext.Default.CreateTransactionCacheEntry);
        RinhaBackendCache.Database.SortedSetAdd(key, cacheEntry, entry.Transaction.RealizadaEm.ToUnixTimeMicroseconds(), CommandFlags.FireAndForget);
    }

    private static async Task Flush(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = RinhaBackendCache.Database.CreateBatch();
            foreach (var (_, channel) in _buffers)
            {
                var count = 0;
                while (count < 10 && channel.Reader.TryRead(out var entry))
                {
                    var key = $"user:{entry.ClientId}:last_transactions";
                    var cacheEntry = JsonSerializer.Serialize(entry, AppJsonSerializerContext.Default.CreateTransactionCacheEntry);
                    batch.SortedSetAddAsync(key, cacheEntry, entry.Transaction.RealizadaEm.ToUnixTimeMicroseconds(), CommandFlags.FireAndForget);
                    count++;
                }
            }
            batch.Execute();

            await Task.Delay(1000, CancellationToken.None);
        }
    }

    private static int CacheWriteThroughput = 0;
    private static async Task CalculateThroughputBySecond(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!shouldUseBuffer && CacheWriteThroughput >= 15)
                shouldUseBuffer = true;
            if (CacheWriteThroughput < 15 && shouldUseBuffer)
                shouldUseBuffer = false;

            Interlocked.Exchange(ref CacheWriteThroughput, 0);

            await Task.Delay(1000, cancellationToken);
        }
    }
}
