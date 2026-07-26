using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responses;

public record DeskAvailabilityResponse
{
    public int DeskId { get; init; }
    public int SlotSizeMinutes { get; init; }
    public IReadOnlyList<AvailabilityIntervalDto> Intervals { get; init; } = [];
}
