// Infrastructure/Synchronization/InMemoryBookingSynchronizer.cs
using Coworking.Application.Common.Synchronization;
using Coworking.Domain.Specifications;
using Nito.AsyncEx;

namespace Coworking.Infrastructure.Synchronization;

public sealed class InMemoryBookingSynchronizer : IBookingSynchronizer
{
    private readonly Dictionary<RangeKey, ActiveRange> _active = [];
    private readonly AsyncLock _lock = new();

    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public async Task<IAsyncDisposable> AcquireAsync(
        Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            List<SemaphoreSlim> toWait;

            using (await _lock.LockAsync(ct))
            {
                toWait = _active.Values
                    .Where(r => r.DeskId == deskId
                             && DateRangeOverlap.Check(start, end, r.Start, r.End))
                    .Select(r => r.Semaphore)
                    .ToList();

                if (toWait.Count == 0)
                {
                    var key = MakeKey(deskId, start, end);

                    _active[key] = new ActiveRange(
                        deskId,
                        start,
                        end,
                        new SemaphoreSlim(0, 1),
                        DateTimeOffset.UtcNow.Add(Ttl));

                    return new RangeHandle(_active, _lock, key);
                }
            }

            // wait outside the lock
            foreach (var semaphore in toWait)
                await semaphore.WaitAsync(ct);
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