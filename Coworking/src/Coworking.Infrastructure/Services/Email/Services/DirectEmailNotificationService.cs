using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Infrastructure.Services.Email.Templates.Models;
using Microsoft.Extensions.Logging;

namespace Coworking.Infrastructure.Services.Email.Services;

/// <summary>
/// Sends emails directly inside the caller's execution context (no Channel, no background worker).
/// Retry is handled at the transport level by MassTransit (UseDelayedRedelivery + UseMessageRetry).
/// </summary>
internal sealed class DirectEmailNotificationService(
    IEmailTemplateService templateService,
    IEmailSender emailSender,
    ILogger<DirectEmailNotificationService> logger) : IEmailNotificationService
{
    public async Task SendBookingCreatedAsync(BookingCreatedEmailModel model, CancellationToken ct)
    {
        var body = await templateService.RenderTemplateFromHbsFileAsync(
            "booking-created.hbs",
            new BookingCreatedTemplateModel(
                To: model.To,
                UserName: model.UserName,
                DeskName: model.DeskName,
                CoworkingName: model.CoworkingName,
                FormattedStart: model.FormattedStart,
                FormattedEnd: model.FormattedEnd,
                TimeZoneId: model.TimeZoneId));

        await emailSender.SendRawEmailAsync(
            model.To,
            $"Booking created — {model.CoworkingName}. Waiting for payment confirmation.",
            body, ct);

        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("Email sent to {To}", model.To);
    }

    public async Task SendBookingCancelledAsync(BookingCancelledEmailModel model, CancellationToken ct)
    {
        var body = await templateService.RenderTemplateFromHbsFileAsync(
            "booking-cancelled.hbs",
            new BookingCancelledTemplateModel(
                To: model.To,
                UserName: model.UserName,
                DeskName: model.DeskName,
                CoworkingName: model.CoworkingName,
                FormattedStart: model.FormattedStart,
                FormattedEnd: model.FormattedEnd,
                TimeZoneId: model.TimeZoneId,
                CancellationReason: model.CancellationReason));

        await emailSender.SendRawEmailAsync(
            model.To,
            $"Booking cancelled — {model.CoworkingName}",
            body, ct);

        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("Email sent to {To}", model.To);
    }
}
