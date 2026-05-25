using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create.Notifications;

// Domain Event — внутреннее событие Application слоя.
// Обработка email вынесена в Coworking.Messaging (Consumer через RabbitMQ).
public sealed record BookingCreatedNotification(
    string UserEmail,
    string UserName,
    string DeskName,
    string CoworkingName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string TimeZoneId) : INotification;
