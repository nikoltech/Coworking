using Coworking.Application.Common.Synchronization;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Synchronization.InMemory.Internal;
using Nito.AsyncEx;

namespace Coworking.Infrastructure.Synchronization.InMemory;

public sealed class InMemoryBookingSynchronizer : IBookingSynchronizer
{
    private readonly Dictionary<RangeKey, ActiveRange> _active = [];
    private readonly AsyncLock _lock = new();
    private readonly TimeProvider _timeProvider;

    private static readonly TimeSpan DefaultAcquireTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BufferLifeTime = TimeSpan.FromMinutes(1);

    public InMemoryBookingSynchronizer(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IAsyncDisposable> AcquireAsync(
        Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
        await AcquireAsync(DefaultAcquireTimeout, deskId, start, end, ct);

    public async Task<IAsyncDisposable> AcquireAsync(TimeSpan? ttl,
        Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            Task[] tasksToWait;

            using (await _lock.LockAsync(ct))
            {
                var overlapping = _active.Values
                    .Where(r => r.DeskId == deskId
                             && DateRangeOverlap.Check(start, end, r.Start, r.End))
                    .Select(r => r.Semaphore)
                    .ToList();

                if (overlapping.Count == 0)
                {
                    var key = MakeKey(deskId, start, end);

                    var expiresAt = _timeProvider.GetUtcNow().Add(ttl ?? DefaultAcquireTimeout + BufferLifeTime);

                    _active[key] = new ActiveRange(
                        deskId,
                        start,
                        end,
                        new SemaphoreSlim(0, 1),
                        expiresAt);

                    return new RangeHandle(_active, _lock, key);
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
            var expired = _active
                .Where(kvp => kvp.Value.ExpiresAt <= DateTimeOffset.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _active[key].Semaphore.Release();
                _active.Remove(key);
            }
        }
    }

    //private static string MakeKey(Guid deskId, DateTimeOffset start, DateTimeOffset end) =>
    //    $"{deskId}:{start:O}:{end:O}";

    private static RangeKey MakeKey(Guid deskId, DateTimeOffset start, DateTimeOffset end) =>
        new(deskId, start, end);
}