namespace Coworking.Infrastructure.Services.Email.Messaging.Dtos;

/// <param name="TraceParent">
/// W3C id of the request that queued the message. A channel hands work to another execution
/// context, which does not carry it.
/// </param>
public record EmailMessageChannelDto(string To, string Subject, string Body, string? TraceParent);
