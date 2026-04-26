using Coworking.Application.Features.Bookings.Commands.Create.Requests;
using Coworking.Application.Features.Bookings.Commands.Create.Responces;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

public record CreateBookingCommand(
    int DeskId,
    //Guid UserId,
    string UserEmail,
    string UserName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingMetadata? Metadata) : IRequest<CreateBookingCommandResponse>;