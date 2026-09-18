using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Services.SlotGenerator;

/// <summary>
/// Cuts a working window into slots of equal real length, so a DST day simply holds one slot
/// fewer or more. Borders carry the coworking's offset at that moment.
/// </summary>
public sealed class SlotGenerator : ISlotGenerator
{
    public IReadOnlyList<TimeSlot> GenerateSlots(DateOnly date, WorkingSchedule schedule)
    {
        var step = schedule.SlotSize.Value;
        var slots = new List<TimeSlot>();

        foreach (var window in schedule.WindowsBetween(date, date))
        {
            var start = window.Start;

            while (start + step <= window.End)
            {
                var end = start + step;

                slots.Add(new TimeSlot(schedule.ToLocal(start), schedule.ToLocal(end)));

                start = end;
            }
        }

        return slots;
    }
}
