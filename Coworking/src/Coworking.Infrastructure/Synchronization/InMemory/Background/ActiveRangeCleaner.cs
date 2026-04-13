using Microsoft.Extensions.Hosting;

namespace Coworking.Infrastructure.Synchronization.InMemory.Background;

internal sealed class ActiveRangeCleaner(InMemoryBookingSynchronizer synchronizer)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(3);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(Interval, ct);
            await synchronizer.CleanExpiredAsync();
        }
    }
}