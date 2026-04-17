using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Services.SlotGenerator;

public sealed class SlotGenerator : ISlotGenerator
{
    // TODO: generated with many iterations. Revise manually again to ensure correctness.
    // 
    // may be issues with cross-season transition periods.
    public IReadOnlyList<TimeSlot> GenerateSlots(
        DateOnly targetDate,
        TimeOnly openTime,
        TimeOnly closeTime,
        SlotSize slotSize,
        string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var openingLocalTime = targetDate.ToDateTime(openTime);
        var closingLocalTime = ResolveClosingBoundary(
            targetDate,
            openTime,
            closeTime);

        var generatedSlots = new List<TimeSlot>();

        var currentSlotStart = openingLocalTime;
        var slotDuration = slotSize.Value;

        while (currentSlotStart + slotDuration <= closingLocalTime)
        {
            var currentSlotEnd = currentSlotStart + slotDuration;

            if (TryCreateSlot(
                timeZone,
                currentSlotStart,
                currentSlotEnd,
                out var slot))
            {
                generatedSlots.Add(slot);
            }

            currentSlotStart = currentSlotEnd;
        }

        return generatedSlots;
    }

    private static DateTime ResolveClosingBoundary(
        DateOnly targetDate,
        TimeOnly openTime,
        TimeOnly closeTime)
    {
        var openingDateTime = targetDate.ToDateTime(openTime);

        var isTwentyFourHoursMode = openTime == closeTime;
        if (isTwentyFourHoursMode)
        {
            return openingDateTime.AddDays(1);
        }

        var closesNextDay = closeTime < openTime;
        if (closesNextDay)
        {
            return targetDate
                .AddDays(1)
                .ToDateTime(closeTime);
        }

        return targetDate.ToDateTime(closeTime);
    }

    private static bool TryCreateSlot(
        TimeZoneInfo timeZone,
        DateTime localStart,
        DateTime localEnd,
        out TimeSlot slot)
    {
        slot = default;

        if (timeZone.IsInvalidTime(localStart))
            return false;

        if (timeZone.IsInvalidTime(localEnd))
            return false;

        var zonedStart = CreateOffsetDateTime(timeZone, localStart);
        var zonedEnd = CreateOffsetDateTime(timeZone, localEnd);

        slot = new TimeSlot(zonedStart, zonedEnd);
        return true;
    }

    private static DateTimeOffset CreateOffsetDateTime(
        TimeZoneInfo timeZone,
        DateTime localDateTime)
    {
        var utcOffset = timeZone.GetUtcOffset(localDateTime);

        return new DateTimeOffset(
            localDateTime,
            utcOffset);
    }
}