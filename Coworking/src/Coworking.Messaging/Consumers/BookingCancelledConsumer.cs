using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Helpers;
using Coworking.Messaging.Contracts;
using MassTransit;

namespace Coworking.Messaging.Consumers;

// Получает Integration Event из RabbitMQ и отправляет email — асинхронная замена
// синхронному BookingCancelledNotificationHandler, который был в Application слое.
//
// ВАЖНО: консьюмер должен быть идемпотентным.
// При наличии хранилища обработанных ID — проверяй context.Message.MessageId перед отправкой.
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
