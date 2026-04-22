namespace Coworking.Application.Ports.Email.Messaging.Dtos;

public record EmailMessageChannelDto(string To, string Subject, string Body);
