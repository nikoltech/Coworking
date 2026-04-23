using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Infrastructure.Services.Email.Messaging.Dtos;
using Coworking.Infrastructure.Services.Email.Messaging.Interfaces;
using Coworking.Infrastructure.Services.Email.Templates.Models;

namespace Coworking.Infrastructure.Services.Email.Services;

internal sealed class EmailNotificationService(
    IEmailTemplateService templateService,
    IEmailChannel channel) : IEmailNotificationService
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
                TimeZoneId: model.TimeZoneId
                ));

        await channel.WriteAsync(new EmailMessageChannelDto(
            To: model.To,
            Subject: $"Booking created — {model.CoworkingName}. Waiting for payment confirmation.",
            Body: body), ct);
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
                CancellationReason: model.CancellationReason
                ));

        await channel.WriteAsync(new EmailMessageChannelDto(
            To: model.To,
            Subject: $"Booking cancelled — {model.CoworkingName}",
            Body: body), ct);
    }
}