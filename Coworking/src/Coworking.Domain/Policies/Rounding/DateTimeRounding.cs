using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Policies.Rounding;

/// <summary>
/// Rounds on UtcTicks so the result is independent of the input offset.
/// </summary>
public static class DateTimeRounding
{
    public static DateTimeOffset FloorToSlot(DateTimeOffset value, SlotSize slotSize)
    {
        long ticks = value.UtcTicks;
        long slotTicks = slotSize.Value.Ticks;

        if (ticks % slotTicks == 0)
            return value;

        long roundedTicks = (ticks / slotTicks) * slotTicks;

        return new DateTimeOffset(roundedTicks, TimeSpan.Zero).ToOffset(value.Offset);
    }

    public static DateTimeOffset CeilToSlot(DateTimeOffset value, SlotSize slotSize)
    {
        long ticks = value.UtcTicks;
        long slotTicks = slotSize.Value.Ticks;

        if (ticks % slotTicks == 0)
            return value;

        // round up without a branch: (x + n - 1) / n * n
        long roundedTicks = ((ticks + slotTicks - 1) / slotTicks) * slotTicks;

        return new DateTimeOffset(roundedTicks, TimeSpan.Zero).ToOffset(value.Offset);
    }
}
