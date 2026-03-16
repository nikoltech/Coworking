using MediatR;

namespace Coworking.Application.Features.Bookings.Commands;

public class CreateBookingCommand : IRequest<Guid>
{
    public Guid DeskId { get; init; }

    public Guid UserId { get; init; }

    public DateTime Start { get; init; }

    public DateTime End { get; init; }

    public CreateBookingCommand(Guid deskId, Guid userId, DateTime start, DateTime end)
    {
        DeskId = deskId;
        UserId = userId;
        Start = start;
        End = end;
    }
}


