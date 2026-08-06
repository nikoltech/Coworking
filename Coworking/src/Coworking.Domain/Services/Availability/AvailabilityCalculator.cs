using Coworking.Domain.Common;
using Coworking.Domain.Specifications;

namespace Coworking.Domain.Services.Availability;

/// <summary>
/// Splits each day's working window into free and busy intervals.
/// </summary>
public sealed class AvailabilityCalculator : IAvailabilityCalculator
{
    public IReadOnlyList<AvailabilityInterval> Calculate(
        DateOnly from, DateOnly to,
        TimeOnly openTime, TimeOnly closeTime,
        TimeZoneInfo timeZone,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy)
    {
        // sorted once: SubtractBusy relies on the order, and sorting per day was the hot spot
        var ordered = OrderByStart(busy);

        var result = new List<AvailabilityInterval>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var (start, end) = ResolveDayWindow(date, openTime, closeTime, timeZone);

            if (start < end)
                result.AddRange(SubtractBusy(start, end, ordered, timeZone));
        }

        return result;
    }

    private static (DateTimeOffset Start, DateTimeOffset End)[] OrderByStart(
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy)
    {
        var ordered = busy.ToArray();

        Array.Sort(ordered, static (left, right) => left.Start.CompareTo(right.Start));

        return ordered;
    }

    /// <summary>
    /// The working period as a moment range. DST changes its real length —
    /// 23 hours on spring forward, 25 on fall back — but never breaks it apart.
    /// </summary>
    private static (DateTimeOffset Start, DateTimeOffset End) ResolveDayWindow(
        DateOnly date, TimeOnly openTime, TimeOnly closeTime, TimeZoneInfo timeZone) =>
        (ZonedTime.FromWallClock(date.ToDateTime(openTime), timeZone),
         ZonedTime.FromWallClock(ResolveLocalEnd(date, openTime, closeTime), timeZone));

    private static DateTime ResolveLocalEnd(DateOnly date, TimeOnly openTime, TimeOnly closeTime)
    {
        // 24/7
        if (openTime == closeTime)
            return date.ToDateTime(openTime).AddDays(1);

        // midnight crossing (22:00 – 06:00)
        if (closeTime < openTime)
            return date.AddDays(1).ToDateTime(closeTime);

        return date.ToDateTime(closeTime);
    }

    private static IEnumerable<AvailabilityInterval> SubtractBusy(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        (DateTimeOffset Start, DateTimeOffset End)[] ordered,
        TimeZoneInfo timeZone)
    {
        // on a normal day every moment shares the window's offset; only a transition needs a lookup
        var sameOffsetAllDay = windowStart.Offset == windowEnd.Offset;

        DateTimeOffset Label(DateTimeOffset moment) =>
            sameOffsetAllDay ? moment.ToOffset(windowStart.Offset) : TimeZoneInfo.ConvertTime(moment, timeZone);

        // clip to the window and collapse touching bookings into busy runs
        var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var (bookedStart, bookedEnd) in ordered)
        {
            if (bookedStart >= windowEnd)
                break;

            if (!DateRangeOverlap.Check(windowStart, windowEnd, bookedStart, bookedEnd))
                continue;

            var start = bookedStart > windowStart ? Label(bookedStart) : windowStart;
            var end = bookedEnd < windowEnd ? Label(bookedEnd) : windowEnd;

            var last = merged.Count - 1;

            if (last >= 0 && start <= merged[last].End)
            {
                if (end > merged[last].End)
                    merged[last] = (merged[last].Start, end);

                continue;
            }

            merged.Add((start, end));
        }

        // walk the window, emitting the free gaps between busy runs
        var cursor = windowStart;

        foreach (var busyRange in merged)
        {
            if (busyRange.Start > cursor)
                yield return new AvailabilityInterval(cursor, busyRange.Start, true);

            yield return new AvailabilityInterval(busyRange.Start, busyRange.End, false);

            cursor = busyRange.End;
        }

        if (cursor < windowEnd)
            yield return new AvailabilityInterval(cursor, windowEnd, true);
    }
}
