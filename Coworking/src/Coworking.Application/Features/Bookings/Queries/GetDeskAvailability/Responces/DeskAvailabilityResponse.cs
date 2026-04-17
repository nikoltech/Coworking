using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responces;

public record DeskAvailabilityResponse
{
    public int DeskId { get; init; }
    public IReadOnlyList<TimeSlotDto> AvailableSlots { get; init; } = [];
}
