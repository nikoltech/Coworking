using Microsoft.Extensions.Hosting;

namespace Coworking.Infrastructure.Synchronization.InMemory.Background;

internal sealed class BookingLockExpiryCleaner(InMemoryBookingAccessCoordinator synchronizer)
    : BackgroundService
{
    private static readonly TimeSpan Interval = InMemoryBookingAccessCoordinator.DefaultAcquireTimeout * 2;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (ct.IsCancellationRequested is false)
        {
            await Task.Delay(Interval, ct);
            await synchronizer.CleanExpiredAsync();
        }
    }
}