using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

namespace Coworking.API.Models.Responces;

public record DeskAvailabilityResponse
{
    public int DeskId { get; init; }
    public IReadOnlyList<TimeSlotResponse> Slots { get; init; } = [];
    public int TotalSlots => Slots.Count;
    public int AvailableSlots => Slots.Count(s => s.IsAvailable);
}
