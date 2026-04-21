using Coworking.Application.Abstractions.Messaging;
using MediatR;

namespace Coworking.Application.Notifications.Email;

public record EmailNotification(string To, string Subject, string Body) : INotification;

public sealed class EmailNotificationHandler(IEmailChannel emailChannel)
    : INotificationHandler<EmailNotification>
{
    public async Task Handle(EmailNotification notification, CancellationToken ct)
    {
        await emailChannel.WriteAsync(notification, ct);
    }
}
