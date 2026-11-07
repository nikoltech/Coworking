using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Coworking.Infrastructure.Synchronization.InMemory.Background;

internal sealed class BookingLockExpiryCleaner(
    ILogger<BookingLockExpiryCleaner> logger,
    InMemoryBookingAccessCoordinator synchronizer,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly TimeSpan Interval = InMemoryBookingAccessCoordinator.DefaultAcquireTimeout * 2;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Interval, timeProvider, stoppingToken);

            while (stoppingToken.IsCancellationRequested is false)
            {
                try
                {
                    await synchronizer.CleanExpiredAsync();
                }

                // a rethrow would stop the whole host; the next pass retries anyway
                catch (Exception ex)
                {
                    logger.LogError(ex, "Booking lock cleanup failed; retrying on the next pass");
                }

                await Task.Delay(Interval, timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        { }
    }
}
