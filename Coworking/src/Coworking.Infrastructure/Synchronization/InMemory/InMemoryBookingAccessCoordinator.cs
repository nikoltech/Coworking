using Coworking.Application.Abstractions.Synchronization;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Synchronization.InMemory.Internal;
using Nito.AsyncEx;
using System.Collections.Concurrent;

namespace Coworking.Infrastructure.Synchronization.InMemory;

/// <summary>
/// Serializes overlapping booking attempts for the same desk in-process, so the DB sees
/// fewer conflicts. Advisory only — correctness rests on the Serializable transaction.
/// </summary>
public sealed class InMemoryBookingAccessCoordinator : IBookingAccessCoordinator
{
    private sealed class DeskLane
    {
        public AsyncLock Lock { get; } = new();
        public Dictionary<RangeKey, ActiveRange> Ranges { get; } = [];
    }

    // lanes are never evicted: dropping an empty one races with acquiring it,
    // and an idle lane costs less than that synchronization
    private readonly ConcurrentDictionary<int, DeskLane> _lanes = new();
    private readonly TimeProvider _timeProvider;

    private static readonly TimeSpan BufferLifeTime = TimeSpan.FromMinutes(1);

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

    public async Task<IAsyncDisposable> WaitIfOverlappingAsync(
        TimeSpan? ttl,
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        var lane = _lanes.GetOrAdd(deskId, static _ => new DeskLane());

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            List<Task>? tasksToWait;

            using (await lane.Lock.LockAsync(ct))
            {
                // stays null while nothing overlaps — the common path allocates nothing
                tasksToWait = null;

                foreach (var range in lane.Ranges.Values)
                {
                    if (DateRangeOverlap.Check(start, end, range.Start, range.End))
                        (tasksToWait ??= []).Add(range.Semaphore.WaitAsync(ct));
                }

                if (tasksToWait is null)
                {
                    var key = MakeKey(deskId, start, end);

                    var expiresAt = _timeProvider.GetUtcNow().Add(ttl ?? DefaultAcquireTimeout + BufferLifeTime);

                    lane.Ranges[key] = new ActiveRange(
                        deskId,
                        start,
                        end,
                        new SemaphoreSlim(0, 1),
                        expiresAt);

                    return new RangeLease(lane.Ranges, lane.Lock, key);
                }
            }

            // wait outside the lock
            await Task.WhenAll(tasksToWait)
                .WaitAsync(ttl ?? DefaultAcquireTimeout, ct);
        }
    }

    internal async Task CleanExpiredAsync()
    {
        var now = _timeProvider.GetUtcNow();

        // each lane is locked briefly and independently
        foreach (var lane in _lanes.Values)
        {
            // TODO: avoid unnecessary global locks and shutdown wait time. Ensure enter range stop_grace_period/SIGTERM !!
            using (await lane.Lock.LockAsync())
            {
                List<RangeKey>? expired = null;

                foreach (var (key, range) in lane.Ranges)
                {
                    if (range.ExpiresAt <= now)
                        (expired ??= []).Add(key);
                }

                if (expired is null)
                    continue;

                foreach (var key in expired)
                {
                    lane.Ranges[key].Semaphore.Release();
                    lane.Ranges.Remove(key);
                }
            }
        }
    }

    private static RangeKey MakeKey(int deskId, DateTimeOffset start, DateTimeOffset end) =>
        new(deskId, start, end);
}
