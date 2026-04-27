namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

public record TimeSlotDto
{
    public TimeSlotDto(DateTimeOffset start, DateTimeOffset end, bool IsAvailable)
    {
        Start = start;
        End = end;
        this.IsAvailable = IsAvailable;
    }

    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public bool IsAvailable { get; init; }
}