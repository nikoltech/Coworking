namespace Coworking.Application.Ports.Synchronization;

public interface IBookingAccessCoordinator
{
    Task<IAsyncDisposable> WaitIfOverlappingAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct);
    Task<IAsyncDisposable> WaitIfOverlappingAsync(
        TimeSpan? ttl,
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct);
}
