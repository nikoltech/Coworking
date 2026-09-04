using Coworking.Application.Ports.Synchronization;

namespace Coworking.IntegrationTests;

/// <summary>
/// Lets overlapping requests reach the database at the same time, so the Serializable
/// conflict path (40001) is exercised instead of the in-process lease short-circuiting it.
/// </summary>
internal sealed class NoOpBookingAccessCoordinator : IBookingAccessCoordinator
{
    public Task<IAsyncDisposable> WaitIfOverlappingAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct) =>
        Task.FromResult<IAsyncDisposable>(NoOpLease.Instance);

    public Task<IAsyncDisposable> WaitIfOverlappingAsync(
        TimeSpan? ttl,
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct) =>
        Task.FromResult<IAsyncDisposable>(NoOpLease.Instance);

    private sealed class NoOpLease : IAsyncDisposable
    {
        public static readonly NoOpLease Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
