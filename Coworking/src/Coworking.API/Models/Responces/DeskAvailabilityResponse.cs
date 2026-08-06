namespace Coworking.API.Models.Responces;

public record DeskAvailabilityResponse
{
    public int DeskId { get; init; }
    public int SlotSizeMinutes { get; init; }
    public IReadOnlyList<AvailabilityIntervalResponse> Intervals { get; init; } = [];

    // counted once from durations; there is no slot grid
    public int TotalSlots { get; init; }
    public int AvailableSlots { get; init; }
}
