using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Policies.Rounding;

public interface IBookingRoundingPolicy
{
    /// <summary>
    /// RoundToSlotGrid snaps the requested period to the slot grid of its working windows.
    /// The end never goes past closing time; moments outside working hours are returned as is.
    /// </summary>
    (DateTimeOffset Start, DateTimeOffset End) RoundToSlotGrid(DateTimeOffset start, DateTimeOffset end, WorkingSchedule schedule);
}
