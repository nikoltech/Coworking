using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Application.Helpers;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create.Notifications;

public sealed record BookingCreatedNotification(
    string UserEmail,
    string UserName,
    string DeskName,
    string CoworkingName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string TimeZoneId) : INotification;

internal sealed class BookingCreatedNotificationHandler(IEmailNotificationService emailService)
    : INotificationHandler<BookingCreatedNotification>
{

    public Task Handle(BookingCreatedNotification n, CancellationToken ct) =>
        emailService.SendBookingCreatedAsync(new BookingCreatedEmailModel(
            To: n.UserEmail,
            UserName: n.UserName,
            DeskName: n.DeskName,
            CoworkingName: n.CoworkingName,
            FormattedStart: BookingDateTimeHelper.FormatDate(n.Start, n.TimeZoneId),
            FormattedEnd: BookingDateTimeHelper.FormatDate(n.End, n.TimeZoneId),
            TimeZoneId: n.TimeZoneId), ct);
}