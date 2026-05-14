using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;
using Coworking.Messaging.Contracts;
using MassTransit;
using MediatR;

namespace Coworking.Messaging.Publishers;

// Слушает Domain Event (MediatR) и транслирует его в Integration Event (RabbitMQ).
// Важно: успешная публикация в брокер НЕ гарантирует обработку консьюмером.
// Для гарантии доставки при сбоях нужен Outbox pattern (запись в БД + фоновая отправка).
internal sealed class BookingCancelledPublisher(IPublishEndpoint publishEndpoint)
    : INotificationHandler<BookingCancelledNotification>
{
    public Task Handle(BookingCancelledNotification n, CancellationToken ct) =>
        publishEndpoint.Publish(new BookingCancelledMessage(
            UserEmail: n.UserEmail,
            UserName: n.UserName,
            DeskName: n.DeskName,
            CoworkingName: n.CoworkingName,
            Start: n.Start,
            End: n.End,
            TimeZoneId: n.TimeZoneId,
            CancellationReason: n.CancellationReason), ct);
}
