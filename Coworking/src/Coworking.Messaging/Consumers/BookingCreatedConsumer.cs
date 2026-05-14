using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Application.Helpers;
using Coworking.Messaging.Contracts;
using MassTransit;

namespace Coworking.Messaging.Consumers;

// Получает Integration Event из RabbitMQ и отправляет email — асинхронная замена
// синхронному BookingCreatedNotificationHandler, который был в Application слое.
//
// Преимущество: HTTP-запрос не ждёт отправки email.
// Если email-сервис упал — retry policy повторит попытку без потери сообщения.
//
// ВАЖНО: консьюмер должен быть идемпотентным.
// При наличии хранилища обработанных ID — проверяй context.Message.MessageId перед отправкой.
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
