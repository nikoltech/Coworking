using Coworking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Domain.Policies.Rounding;

public interface IBookingRoundingPolicy
{
    DateTimeOffset RoundStart(DateTimeOffset start, SlotSize slotSize);
    DateTimeOffset RoundEnd(DateTimeOffset end, SlotSize slotSize);

    (DateTimeOffset Start, DateTimeOffset End) RoundInterval(DateTimeOffset start, DateTimeOffset end, SlotSize slotSize);
}
