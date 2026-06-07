using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Policies.Rounding;

/// <summary> 
/// The policy operates on the principle of "expanding" the interval.
/// Rounds start down (floor) and end up (ceil). 
/// </summary>
public class DefaultRoundingPolicy : IBookingRoundingPolicy
{
    public DateTimeOffset RoundStart(DateTimeOffset start, SlotSize slotSize)
        => DateTimeRounding.FloorToSlot(start, slotSize);

    public DateTimeOffset RoundEnd(DateTimeOffset end, SlotSize slotSize)
        => DateTimeRounding.CeilToSlot(end, slotSize);

    public (DateTimeOffset Start, DateTimeOffset End) RoundInterval(
        DateTimeOffset start, 
        DateTimeOffset end, 
        SlotSize slotSize)
    {
        return (RoundStart(start, slotSize), RoundEnd(end, slotSize));
    }
}
