using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coworking.Infrastructure.Synchronization.InMemory.Background;

internal sealed class BookingLockExpiryCleaner(ILogger<BookingLockExpiryCleaner> logger, InMemoryBookingAccessCoordinator synchronizer)
    : BackgroundService
{
    private static readonly TimeSpan Interval = InMemoryBookingAccessCoordinator.DefaultAcquireTimeout * 2;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            while (ct.IsCancellationRequested is false)
            {
                await Task.Delay(Interval, ct);
                await synchronizer.CleanExpiredAsync();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        { }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "BookingLockExpiryCleaner failed");
            throw;
        }
    }
}