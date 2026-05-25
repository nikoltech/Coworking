namespace Coworking.Messaging.Contracts.Abstracts;

// Integration event — публичный контракт между сервисами.
// В отличие от Domain Event (внутренний, MediatR), пересекает границы сервисов через брокер.
// Правило: события иммутабельны — они описывают факт прошлого, а не намерение.
public abstract record IntegrationEvent
{
    // Уникальный ID сообщения. Консьюмер использует его для идемпотентности:
    // at-least-once delivery означает, что одно событие может прийти дважды.
    public Guid MessageId { get; init; } = Guid.NewGuid();

    // Когда событие произошло в домене (не когда сообщение было отправлено в брокер).
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    // Строка вместо enum — консьюмер не сломается при переименовании типа в продюсере.
    public abstract string EventType { get; }
}
