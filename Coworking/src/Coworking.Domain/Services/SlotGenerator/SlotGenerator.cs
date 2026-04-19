using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Services.SlotGenerator;

public sealed class SlotGenerator : ISlotGenerator
{
    /// <summary>
    /// Generates time slots for a given date in the coworking's local timezone.
    /// DST gaps are skipped entirely — slots overlapping invalid times are excluded.
    /// DST folds use standard (winter) UTC offset for ambiguous times.
    /// </summary>
    public IReadOnlyList<TimeSlot> GenerateSlots(
        DateOnly targetDate,
        TimeOnly openTime,
        TimeOnly closeTime,
        SlotSize slotSize,
        string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var slotLength = slotSize.Value;
        var periodEnd = ResolvePeriodEnd(targetDate, openTime, closeTime);

        var slots = new List<TimeSlot>();
        var current = targetDate.ToDateTime(openTime);

        while (current + slotLength <= periodEnd)
        {
            var next = current + slotLength;

            if (TryBuildSlot(timeZone, current, next, out var slot))
                slots.Add(slot);

            current = next;
        }

        return slots;
    }

    // ── period boundary ─────────────────────────────────────────────────────

    private static DateTime ResolvePeriodEnd(
        DateOnly date, TimeOnly openTime, TimeOnly closeTime)
    {
        // 24/7 mode
        if (openTime == closeTime)
            return date.ToDateTime(openTime).AddDays(1);

        // midnight crossing (e.g. 22:00 – 06:00)
        if (closeTime < openTime)
            return date.AddDays(1).ToDateTime(closeTime);

        // regular hours (e.g. 08:00 – 20:00)
        return date.ToDateTime(closeTime);
    }

    // ── slot construction ────────────────────────────────────────────────────

    private static bool TryBuildSlot(
        TimeZoneInfo timeZone,
        DateTime localStart,
        DateTime localEnd,
        out TimeSlot slot)
    {
        slot = default;

        // DST gap — these local times do not exist, skip the slot
        if (timeZone.IsInvalidTime(localStart) || timeZone.IsInvalidTime(localEnd))
            return false;

        slot = new TimeSlot(
            ToZonedOffset(timeZone, localStart),
            ToZonedOffset(timeZone, localEnd));

        return true;
    }

    /// <summary>
    /// Converts a local DateTime to DateTimeOffset using the timezone's UTC offset.
    /// For ambiguous times (DST fold), uses the standard (winter) offset
    /// to avoid non-deterministic results.
    /// </summary>
    private static DateTimeOffset ToZonedOffset(TimeZoneInfo timeZone, DateTime local)
    {
        var offset = timeZone.IsAmbiguousTime(local)
            ? timeZone.BaseUtcOffset
            : timeZone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset);
    }
}