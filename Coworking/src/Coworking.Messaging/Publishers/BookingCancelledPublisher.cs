using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;
using Coworking.Messaging.Contracts;
using MassTransit;
using MediatR;

namespace Coworking.Messaging.Publishers;

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
