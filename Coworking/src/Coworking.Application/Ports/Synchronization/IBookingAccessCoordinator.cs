namespace Coworking.Application.Ports.Synchronization;

public interface IBookingAccessCoordinator
{
    /// <summary>
    /// WaitIfOverlappingAsync holds the desk interval until the returned lease is disposed,
    /// waiting out the overlapping leases first. Advisory only: the database still decides.
    /// </summary>
    /// <exception cref="Coworking.Application.Common.Exceptions.ServiceBusyException">
    /// The waiting budget ran out.
    /// </exception>
    Task<IAsyncDisposable> WaitIfOverlappingAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct);

    /// <inheritdoc cref="WaitIfOverlappingAsync(int, DateTimeOffset, DateTimeOffset, CancellationToken)"/>
    /// <param name="ttl">The whole waiting budget of this request, across every wait it makes.</param>
    Task<IAsyncDisposable> WaitIfOverlappingAsync(
        TimeSpan? ttl,
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct);
}
