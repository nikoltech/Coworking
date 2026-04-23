namespace Coworking.Infrastructure.Services.Email.Messaging.Dtos;

public record EmailMessageChannelDto(string To, string Subject, string Body);
