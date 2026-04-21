using Coworking.Application.Notifications.Email;

namespace Coworking.Application.Abstractions.Messaging;

public interface IEmailChannel
{
    ValueTask WriteAsync(EmailNotification notification, CancellationToken ct);
}
