using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.CreateBooking;

public record CreateBookingCommand : IRequest<Guid>
{
    public Guid DeskId { get; init; }

    public Guid UserId { get; init; }

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }

    public int TimeZoneId { get; init; }

    public CreateBookingCommand(Guid deskId, Guid userId, DateTimeOffset startTime, DateTimeOffset endTime, int timeZoneId)
    {
        DeskId = deskId;
        UserId = userId;
        StartTime = startTime;
        EndTime = endTime;
        TimeZoneId = timeZoneId;
    }
}


