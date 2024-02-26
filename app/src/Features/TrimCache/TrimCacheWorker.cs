
using StackExchange.Redis;

namespace RinhaBackend;

public class TrimCacheWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            var keys = await RinhaBackendCache.Database.ExecuteAsync("KEYS", "user:*:last_transactions");
            if (keys is null)
                continue;

            var batch = RinhaBackendCache.Database.CreateBatch();
            var keysAsString = (string[])keys!;
            foreach (var key in keysAsString)
            {
                _ = batch.SortedSetRemoveRangeByRankAsync(key, 0, -11, CommandFlags.FireAndForget);
            }
            batch.Execute();
        }
    }
}
