namespace Coworking.Messaging.Contracts.Abstracts;

public abstract record IntegrationEvent
{
    // when it happened in the domain, not when it was published
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    // string, not enum: renaming the producer type must not break consumers
    public abstract string EventType { get; }
}
