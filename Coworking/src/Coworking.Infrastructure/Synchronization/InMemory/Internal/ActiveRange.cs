namespace Coworking.Infrastructure.Synchronization.InMemory.Internal;

internal sealed record ActiveRange(
    int DeskId,
    DateTimeOffset Start,
    DateTimeOffset End,
    SemaphoreSlim Semaphore,
    DateTimeOffset ExpiresAt);
