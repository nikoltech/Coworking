using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

public record CreateBookingCommand(
    int DeskId,
    Guid UserId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? UserTimeZoneId) : IRequest<int>
{
    // TODO: add more properties if needed
}