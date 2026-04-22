using Coworking.Application.Ports.Email.Messaging.Dtos;

namespace Coworking.Infrastructure.Services.Email.Messaging.Interfaces;

public interface IEmailChannel
{
    ValueTask WriteAsync(EmailMessageChannelDto notification, CancellationToken ct);
}
