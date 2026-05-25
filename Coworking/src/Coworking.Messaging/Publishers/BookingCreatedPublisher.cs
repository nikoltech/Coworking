using Coworking.Application.Features.Bookings.Commands.Create.Notifications;
using Coworking.Messaging.Contracts;
using MassTransit;
using MediatR;

namespace Coworking.Messaging.Publishers;

// Слушает Domain Event (MediatR) и транслирует его в Integration Event (RabbitMQ).
// Это граница между внутренней моделью и публичным контрактом.
//
// Важно: успешная публикация в брокер НЕ гарантирует обработку консьюмером.
// Для гарантии доставки при сбоях нужен Outbox pattern (запись в БД + фоновая отправка).
internal sealed class BookingCreatedPublisher(IPublishEndpoint publishEndpoint)
    : INotificationHandler<BookingCreatedNotification>
{
    public Task Handle(BookingCreatedNotification n, CancellationToken ct) =>
        publishEndpoint.Publish(new BookingCreatedMessage(
            UserEmail: n.UserEmail,
            UserName: n.UserName,
            DeskName: n.DeskName,
            CoworkingName: n.CoworkingName,
            Start: n.Start,
            End: n.End,
            TimeZoneId: n.TimeZoneId), ct);
}
