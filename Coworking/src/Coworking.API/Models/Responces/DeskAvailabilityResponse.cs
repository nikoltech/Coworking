namespace Coworking.API.Models.Responces;

public record DeskAvailabilityResponse
{
    public int DeskId { get; init; }
    public int SlotSizeMinutes { get; init; }
    public IReadOnlyList<AvailabilityIntervalResponse> Intervals { get; init; } = [];

    public int TotalSlots => CountSlots(Intervals);
    public int AvailableSlots => CountSlots(Intervals.Where(i => i.IsAvailable));

    // counts derived from durations; there is no slot grid
    private int CountSlots(IEnumerable<AvailabilityIntervalResponse> intervals) =>
        intervals.Sum(i => (int)((i.End - i.Start).TotalMinutes / SlotSizeMinutes));
}
