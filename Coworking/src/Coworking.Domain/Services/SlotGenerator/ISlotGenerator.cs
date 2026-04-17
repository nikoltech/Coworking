using Coworking.Domain.ValueObjects;
namespace Coworking.Domain.Services.SlotGenerator;

public interface ISlotGenerator
{
    IReadOnlyList<TimeSlot> GenerateSlots(
        DateOnly targetDate,
        TimeOnly openTime,
        TimeOnly closeTime,
        SlotSize slotSize,
        string timeZoneId);
}
