using Coworking.Application.Abstractions.Messaging;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Application.Notifications.Email;
using Coworking.Infrastructure.Services.Email.Templates.Models;

namespace Coworking.Infrastructure.Services.Email.Services;

internal sealed class EmailNotificationService(
    IEmailTemplateService templateService,
    IEmailChannel channel) : IEmailNotificationService
{
    public Task SendBookingCreatedAsync(BookingCreatedEmailModel model, CancellationToken ct)
    {
        var body = templateService.RenderTemplateFromHbsFile("booking-created.hbs", new BookingCreatedTemplateModel(
                To: model.To,
                UserName: model.UserName,
                DeskName: model.DeskName,
                CoworkingName: model.CoworkingName,
                FormattedStart: model.FormattedStart,
                FormattedEnd: model.FormattedEnd,
                TimeZoneId: model.TimeZoneId));

        return channel.WriteAsync(new EmailNotification(
            To: model.To,
            Subject: $"Booking confirmed — {model.CoworkingName}",
            Body: body), ct).AsTask();
    }

    public Task SendBookingCancelledAsync(BookingCancelledEmailModel model, CancellationToken ct)
    {
        var body = templateService.RenderTemplateFromHbsFile("booking-cancelled.hbs", new BookingCancelledTemplateModel(
                To: model.To,
                UserName: model.UserName,
                DeskName: model.DeskName,
                CoworkingName: model.CoworkingName,
                FormattedStart: model.FormattedStart,
                FormattedEnd: model.FormattedEnd,
                TimeZoneId: model.TimeZoneId,
                CancellationReason: model.CancellationReason));

        return channel.WriteAsync(new EmailNotification(
            To: model.To,
            Subject: $"Booking cancelled — {model.CoworkingName}",
            Body: body), ct).AsTask();
    }
}