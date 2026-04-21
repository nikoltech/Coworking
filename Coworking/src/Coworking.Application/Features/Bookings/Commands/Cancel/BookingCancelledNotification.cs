using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Notifications.Email;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Cancel;

public sealed record BookingCancelledNotification(
    string UserEmail,
    string UserName,
    string DeskName,
    string CoworkingName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string TimeZoneId,
    string? CancellationReason) : INotification;

internal sealed class BookingCancelledNotificationHandler(IEmailNotificationService emailService)
    : INotificationHandler<BookingCancelledNotification>
{
    public Task Handle(BookingCancelledNotification n, CancellationToken ct) =>
        emailService.SendBookingCancelledAsync(new BookingCancelledEmailModel(
            To: n.UserEmail,
            UserName: n.UserName,
            DeskName: n.DeskName,
            CoworkingName: n.CoworkingName,
            FormattedStart: n.Start,
            FormattedEnd: n.End,
            TimeZoneId: n.TimeZoneId,
            CancellationReason: n.CancellationReason), ct);
}