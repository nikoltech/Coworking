using Coworking.Application.Abstractions.Synchronization;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Synchronization.InMemory.Internal;
using Nito.AsyncEx;

namespace Coworking.Infrastructure.Synchronization.InMemory;

/// <summary>
/// “soft fairness queue + optimistic contention reducer”
/// </summary>
public sealed class InMemoryBookingAccessCoordinator : IBookingAccessCoordinator
{
    private readonly Dictionary<RangeKey, ActiveRange> _activeRanges = [];
    private readonly AsyncLock _lock = new();
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
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            Task[] tasksToWait;

            using (await _lock.LockAsync(ct))
            {
                var overlapping = _activeRanges.Values
                    .Where(r => r.DeskId == deskId
                             && DateRangeOverlap.Check(start, end, r.Start, r.End))
                    .Select(r => r.Semaphore)
                    .ToList();

                if (overlapping.Count == 0)
                {
                    var key = MakeKey(deskId, start, end);

                    var expiresAt = _timeProvider.GetUtcNow().Add(ttl ?? DefaultAcquireTimeout + BufferLifeTime);

                    _activeRanges[key] = new ActiveRange(
                        deskId,
                        start,
                        end,
                        new SemaphoreSlim(0, 1),
                        expiresAt);

                    return new RangeLease(_activeRanges, _lock, key);
                }

                tasksToWait = overlapping
                    .Select(semaphore => semaphore.WaitAsync(ct))
                    .ToArray();
            }

            // wait outside the lock
            await Task.WhenAll(tasksToWait)
                .WaitAsync(ttl ?? DefaultAcquireTimeout, ct);
        }
    }

    internal async Task CleanExpiredAsync()
    {
        using (await _lock.LockAsync())
        {
            var expired = _activeRanges
                .Where(kvp => kvp.Value.ExpiresAt <= _timeProvider.GetUtcNow())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _activeRanges[key].Semaphore.Release();
                _activeRanges.Remove(key);
            }
        }
    }

    //private static string MakeKey(Guid deskId, DateTimeOffset start, DateTimeOffset end) =>
    //    $"{deskId}:{start:O}:{end:O}";

    private static RangeKey MakeKey(int deskId, DateTimeOffset start, DateTimeOffset end) =>
        new(deskId, start, end);
}