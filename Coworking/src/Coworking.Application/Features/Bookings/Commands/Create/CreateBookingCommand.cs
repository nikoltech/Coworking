using Coworking.Application.Features.Bookings.Commands.Create.Requests;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

public record CreateBookingCommand(
    int DeskId,
    Guid UserId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingMetadata? Metadata) : IRequest<int>;