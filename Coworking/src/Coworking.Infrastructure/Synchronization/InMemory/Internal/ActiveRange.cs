namespace Coworking.Infrastructure.Synchronization.InMemory.Internal;

/// <summary>
/// A held range. Released completes once and wakes every waiter; nothing is consumed,
/// so a waiter that gave up cannot take the wake-up from one still waiting.
/// A class on purpose: removal must match this instance, not an equal range held later.
/// </summary>
internal sealed class ActiveRange(DateTimeOffset start, DateTimeOffset end, DateTimeOffset expiresAt)
{
    public DateTimeOffset Start => start;
    public DateTimeOffset End => end;
    public DateTimeOffset ExpiresAt => expiresAt;

    public TaskCompletionSource Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
