using Nito.AsyncEx;

namespace Coworking.Infrastructure.Synchronization.InMemory.Internal;

internal sealed class RangeLease(
    Dictionary<RangeKey, ActiveRange> active,
    AsyncLock lockObj,
    RangeKey key) : IAsyncDisposable
{
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        SemaphoreSlim? semaphore;

        using (await lockObj.LockAsync())
        {
            active.TryGetValue(key, out var range);
            semaphore = range?.Semaphore;
            active.Remove(key);
        }

        semaphore?.Release();
    }
}