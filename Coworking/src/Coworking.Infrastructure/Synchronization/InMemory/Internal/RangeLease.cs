using Nito.AsyncEx;

namespace Coworking.Infrastructure.Synchronization.InMemory.Internal;

/// <summary>
/// Releasing twice, or after the cleaner reclaimed the range, is a no-op.
/// </summary>
internal sealed class RangeLease(
    List<ActiveRange> held,
    AsyncLock lockObj,
    ActiveRange range) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        using (await lockObj.LockAsync())
            held.Remove(range);

        range.Released.TrySetResult();
    }
}
