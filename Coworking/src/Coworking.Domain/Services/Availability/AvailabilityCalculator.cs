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
        var result = new List<AvailabilityInterval>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var (start, end) = ResolveDayWindow(date, openTime, closeTime, timeZone);

            if (start < end)
                result.AddRange(SubtractBusy(start, end, busy));
        }

        return result;
    }

    /// <summary>
    /// The working period as an instant range. DST changes its real length —
    /// 23 hours on spring forward, 25 on fall back — but never breaks it apart.
    /// </summary>
    private static (DateTimeOffset Start, DateTimeOffset End) ResolveDayWindow(
        DateOnly date, TimeOnly openTime, TimeOnly closeTime, TimeZoneInfo timeZone) =>
        (ToInstant(date.ToDateTime(openTime), timeZone),
         ToInstant(ResolveLocalEnd(date, openTime, closeTime), timeZone));

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

    private static DateTimeOffset ToInstant(DateTime local, TimeZoneInfo timeZone) =>
        new(local, timeZone.GetUtcOffset(local));

    private static IEnumerable<AvailabilityInterval> SubtractBusy(
        DateTimeOffset windowStart, 
        DateTimeOffset windowEnd,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy)
    {
        // keep only what overlaps the window, trimmed to its bounds and to its local offset
        var clipped = busy
            .Where(b => DateRangeOverlap.Check(windowStart, windowEnd, b.Start, b.End))
            .Select(b => (
                Start: b.Start > windowStart ? b.Start.ToOffset(windowStart.Offset) : windowStart,
                End: b.End < windowEnd ? b.End.ToOffset(windowEnd.Offset) : windowEnd))
            .OrderBy(b => b.Start)
            .ToList();

        // collapse overlapping and adjacent bookings into one busy run
        var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var busyRange in clipped)
        {
            if (merged.Count > 0 && busyRange.Start <= merged[^1].End)
            {
                var last = merged[^1];

                if (busyRange.End > last.End)
                    merged[^1] = (last.Start, busyRange.End);

                continue;
            }

            merged.Add(busyRange);
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
