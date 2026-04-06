using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

public record CreateBookingCommand(
    Guid DeskId,
    Guid UserId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string TimeZoneId) : IRequest<Guid>
{
    // TODO: add more properties if needed
}