using Coworking.Messaging.Contracts.Abstracts;

namespace Coworking.Messaging.Contracts;

public sealed record BookingCancelledMessage(
    string UserEmail,
    string UserName,
    string DeskName,
    string CoworkingName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string TimeZoneId,
    string? CancellationReason) : IntegrationEvent
{
    public override string EventType => "booking.cancelled";
}
