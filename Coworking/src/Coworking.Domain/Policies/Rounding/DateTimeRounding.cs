using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Policies.Rounding;

/// <summary>
/// UTC Safety
/// </summary>
public static class DateTimeRounding
{
    public static DateTimeOffset FloorToSlot(DateTimeOffset value, SlotSize slotSize)
    {
        long ticks = value.UtcTicks;
        long slotTicks = slotSize.Value.Ticks;

        // If the value is already aligned to the slot, return it as is
        if (ticks % slotTicks == 0)
            return value;

        // Use UtcTicks to ensure linear calculations
        long roundedTicks = (ticks / slotTicks) * slotTicks;

        return new DateTimeOffset(roundedTicks, TimeSpan.Zero).ToOffset(value.Offset);
    }

    public static DateTimeOffset CeilToSlot(DateTimeOffset value, SlotSize slotSize)
    {
        long ticks = value.UtcTicks;
        long slotTicks = slotSize.Value.Ticks;

        // If the value is already aligned to the slot, return it as is
        if (ticks % slotTicks == 0)
            return value;

        // Mathing trick for rounding up without if/else:
        // (x + т - 1) / т * т
        long roundedTicks = ((ticks + slotTicks - 1) / slotTicks) * slotTicks;

        return new DateTimeOffset(roundedTicks, TimeSpan.Zero).ToOffset(value.Offset);
    }
}
