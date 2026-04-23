using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Helpers;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;

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
            FormattedStart: BookingDateTimeHelper.FormatDate(n.Start, n.TimeZoneId),
            FormattedEnd: BookingDateTimeHelper.FormatDate(n.End, n.TimeZoneId),
            TimeZoneId: n.TimeZoneId,
            CancellationReason: n.CancellationReason), ct);
}