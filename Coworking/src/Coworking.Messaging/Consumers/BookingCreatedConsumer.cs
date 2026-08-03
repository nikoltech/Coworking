using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Application.Helpers;
using Coworking.Messaging.Contracts;
using MassTransit;

namespace Coworking.Messaging.Consumers;

// TODO: not idempotent yet: needs a processed-MessageId store keyed on context.Message.MessageId
internal sealed class BookingCreatedConsumer(IEmailNotificationService emailService)
    : IConsumer<BookingCreatedMessage>
{
    public Task Consume(ConsumeContext<BookingCreatedMessage> context)
    {
        var msg = context.Message;

        return emailService.SendBookingCreatedAsync(new BookingCreatedEmailModel(
            To: msg.UserEmail,
            UserName: msg.UserName,
            DeskName: msg.DeskName,
            CoworkingName: msg.CoworkingName,
            FormattedStart: BookingDateTimeHelper.FormatDate(msg.Start, msg.TimeZoneId),
            FormattedEnd: BookingDateTimeHelper.FormatDate(msg.End, msg.TimeZoneId),
            TimeZoneId: msg.TimeZoneId), context.CancellationToken);
    }
}
