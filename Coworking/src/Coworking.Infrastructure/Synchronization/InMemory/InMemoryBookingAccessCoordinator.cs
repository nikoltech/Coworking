using Coworking.Application.Common.Exceptions;
using Coworking.Application.Ports.Synchronization;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Synchronization.InMemory.Internal;
using Nito.AsyncEx;
using System.Collections.Concurrent;

namespace Coworking.Infrastructure.Synchronization.InMemory;

/// <summary>
/// Serializes overlapping booking attempts for the same desk in-process, against held ranges
/// only. Advisory — correctness rests on the Serializable transaction.
/// </summary>
public sealed class InMemoryBookingAccessCoordinator : IBookingAccessCoordinator
{
    private sealed class DeskLane
    {
        public AsyncLock Lock { get; } = new();
        public List<ActiveRange> Held { get; } = [];
    }

    // never evicted: dropping an empty lane races with acquiring it
    private readonly ConcurrentDictionary<int, DeskLane> _lanes = new();
    private readonly TimeProvider _timeProvider;

    private static readonly TimeSpan LeaseLifetime = TimeSpan.FromMinutes(5);

    public InMemoryBookingAccessCoordinator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public static readonly TimeSpan DefaultAcquireTimeout = TimeSpan.FromSeconds(30);

    public async Task<IAsyncDisposable> WaitIfOverlappingAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct) =>
        await WaitIfOverlappingAsync(DefaultAcquireTimeout, deskId, start, end, ct);

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> WaitIfOverlappingAsync(
        TimeSpan? ttl,
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        var lane = _lanes.GetOrAdd(deskId, static _ => new DeskLane());
        var deadline = _timeProvider.GetUtcNow() + (ttl ?? DefaultAcquireTimeout);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            ActiveRange? holder;

            using (await lane.Lock.LockAsync(ct))
            {
                holder = lane.Held.Find(h => DateRangeOverlap.Check(start, end, h.Start, h.End));

                if (holder is null)
                {
                    var acquired = new ActiveRange(start, end, _timeProvider.GetUtcNow() + LeaseLifetime);
                    lane.Held.Add(acquired);

                    return new RangeLease(lane.Held, lane.Lock, acquired);
                }
            }

            // a new holder may appear while waiting, hence the loop
            var remaining = deadline - _timeProvider.GetUtcNow();

            try
            {
                await holder.Released.Task.WaitAsync(Max(remaining, TimeSpan.Zero), _timeProvider, ct);
            }
            catch (TimeoutException ex)
            {
                throw new ServiceBusyException(
                    $"Desk {deskId} is busy for the requested time. Try again later.", ex);
            }
        }
    }

    internal async Task CleanExpiredAsync()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var lane in _lanes.Values)
        {
            // TODO: avoid unnecessary global locks and shutdown wait time. Ensure enter range stop_grace_period/SIGTERM !!
            using (await lane.Lock.LockAsync())
            {
                foreach (var range in lane.Held.Where(r => r.ExpiresAt <= now))
                    range.Released.TrySetResult();

                lane.Held.RemoveAll(r => r.ExpiresAt <= now);
            }
        }
    }

    // -1 ms would mean waiting forever
    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;
}
