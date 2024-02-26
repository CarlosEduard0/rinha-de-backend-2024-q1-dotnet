using System.Collections.Concurrent;
using System.Text.Json;
using StackExchange.Redis;

namespace RinhaBackend;

public static class CreateTransactionCache
{
    private static readonly ConcurrentQueue<CreateTransactionCacheEntry> _buffer = new();

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
            _buffer.Enqueue(entry);

            return;
        }
        
        var key = $"user:{entry.ClientId}:last_transactions";
        var cacheEntry = JsonSerializer.Serialize(entry, AppJsonSerializerContext.Default.CreateTransactionCacheEntry);
        RinhaBackendCache.Database.SortedSetAdd(key, cacheEntry, entry.Transaction.RealizadaEm.ToUnixTimeMicroseconds(), CommandFlags.FireAndForget);
    }

    private static async Task Flush(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested || !_buffer.IsEmpty)
        {
            if (!_buffer.IsEmpty)
            {
                var batch = RinhaBackendCache.Database.CreateBatch();
                var bufferCount = _buffer.Count;
                int elementsDequeuedCount = 0;
                while (elementsDequeuedCount < bufferCount)
                {
                    if (!_buffer.TryDequeue(out var entry))
                        break;
                    var key = $"user:{entry.ClientId}:last_transactions";
                    var cacheEntry = JsonSerializer.Serialize(entry, AppJsonSerializerContext.Default.CreateTransactionCacheEntry);
                    batch.SortedSetAddAsync(key, cacheEntry, entry.Transaction.RealizadaEm.ToUnixTimeMicroseconds(), CommandFlags.FireAndForget);
                    elementsDequeuedCount++;
                }
                batch.Execute();
            }

            await Task.Delay(300, CancellationToken.None);
        }
    }

    private static int CacheWriteThroughput = 0;
    private static async Task CalculateThroughputBySecond(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (CacheWriteThroughput >= 15 && !shouldUseBuffer)
                shouldUseBuffer = true;
            if (CacheWriteThroughput < 15 && shouldUseBuffer)
                shouldUseBuffer = false;

            if (CacheWriteThroughput > 0)
                Interlocked.Exchange(ref CacheWriteThroughput, 0);

            await Task.Delay(1000, cancellationToken);
        }
    }
}
