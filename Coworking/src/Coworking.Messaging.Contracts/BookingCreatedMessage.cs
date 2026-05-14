using Coworking.Messaging.Contracts.Abstracts;

namespace Coworking.Messaging.Contracts;

public sealed record BookingCreatedMessage(
    string UserEmail,
    string UserName,
    string DeskName,
    string CoworkingName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string TimeZoneId) : IntegrationEvent
{
    public override string EventType => "booking.created";
}
