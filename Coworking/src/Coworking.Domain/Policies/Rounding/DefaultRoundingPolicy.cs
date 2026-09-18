using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Policies.Rounding;

/// <summary>
/// The policy operates on the principle of "expanding" the interval.
/// Rounds start down (floor) and end up (ceil).
/// </summary>
public class DefaultRoundingPolicy : IBookingRoundingPolicy
{
    public (DateTimeOffset Start, DateTimeOffset End) RoundToSlotGrid(
        DateTimeOffset start,
        DateTimeOffset end,
        WorkingSchedule schedule)
    {
        var step = schedule.SlotSize.Value;

        var roundedStart = schedule.WindowForStart(start) is { } startWindow
            ? DateTimeRounding.FloorToGrid(start, startWindow.Start, step)
            : start;

        var roundedEnd = schedule.WindowForEnd(end) is { } endWindow
            ? Min(DateTimeRounding.CeilToGrid(end, endWindow.Start, step), endWindow.End)
            : end;

        return (schedule.ToLocal(roundedStart), schedule.ToLocal(roundedEnd));
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) =>
        left <= right ? left : right;
}
