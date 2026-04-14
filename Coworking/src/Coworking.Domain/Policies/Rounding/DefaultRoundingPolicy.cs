using Coworking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Domain.Policies.Rounding;

/// <summary> 
/// Rounds start down (floor) and end up (ceil). 
/// </summary>
public class DefaultRoundingPolicy : IBookingRoundingPolicy
{
    public DateTimeOffset RoundStart(DateTimeOffset start, SlotSize slotSize)
        => DateTimeRounding.FloorToSlot(start, slotSize);

    public DateTimeOffset RoundEnd(DateTimeOffset end, SlotSize slotSize)
        => DateTimeRounding.CeilToSlot(end, slotSize);

    public (DateTimeOffset Start, DateTimeOffset End) RoundInterval(DateTimeOffset start, DateTimeOffset end, SlotSize slotSize)
    {
        return (RoundStart(start, slotSize), RoundEnd(end, slotSize));
    }
}
