using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Helpers;
using Coworking.Messaging.Contracts;
using MassTransit;

namespace Coworking.Messaging.Consumers;

// TODO: not idempotent yet: needs a processed-MessageId store keyed on context.Message.MessageId
internal sealed class BookingCancelledConsumer(IEmailNotificationService emailService)
    : IConsumer<BookingCancelledMessage>
{
    public Task Consume(ConsumeContext<BookingCancelledMessage> context)
    {
        var msg = context.Message;

        return emailService.SendBookingCancelledAsync(new BookingCancelledEmailModel(
            To: msg.UserEmail,
            UserName: msg.UserName,
            DeskName: msg.DeskName,
            CoworkingName: msg.CoworkingName,
            FormattedStart: BookingDateTimeHelper.FormatDate(msg.Start, msg.TimeZoneId),
            FormattedEnd: BookingDateTimeHelper.FormatDate(msg.End, msg.TimeZoneId),
            TimeZoneId: msg.TimeZoneId,
            CancellationReason: msg.CancellationReason), context.CancellationToken);
    }
}
