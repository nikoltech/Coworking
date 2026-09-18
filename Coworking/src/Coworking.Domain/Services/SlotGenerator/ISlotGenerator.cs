using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Services.SlotGenerator;

public interface ISlotGenerator
{
    /// <summary>
    /// GenerateSlots splits the working window opening on this local date into whole slots.
    /// A tail shorter than a slot is left out.
    /// </summary>
    IReadOnlyList<TimeSlot> GenerateSlots(DateOnly date, WorkingSchedule schedule);
}
