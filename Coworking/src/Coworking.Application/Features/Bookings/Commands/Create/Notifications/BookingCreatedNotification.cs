using Coworking.Application.Abstractions;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Application.Notifications.Email;
using Coworking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
            FormattedStart: FormatDate(n.Start, n.TimeZoneId),
            FormattedEnd: FormatDate(n.End, n.TimeZoneId),
            TimeZoneId: n.TimeZoneId), ct);
 
    private static string FormatDate(DateTimeOffset dt, string timeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(dt, zone);
        return local.ToString("dddd, MMMM d yyyy · HH:mm", CultureInfo.InvariantCulture);
    }
}