using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

namespace Coworking.API.Models.Responces;

/// <summary>
/// Represents desk availability information for a specific date.
/// </summary>
public record DeskAvailabilityResponse
{
    /// <summary>
    /// Desk identifier.
    /// </summary>
    public int DeskId { get; init; }

    /// <summary>
    /// Target date for availability calculation.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Available time slots for the desk.
    /// </summary>
    public IReadOnlyList<TimeSlotDto> AvailableSlots { get; init; } = [];

    /// <summary>
    /// Total number of available slots.
    /// </summary>
    public int TotalAvailableSlots => AvailableSlots.Count;
}