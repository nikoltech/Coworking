using Coworking.Domain.Specifications;
using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Services.Availability;

/// <summary>
/// Splits each day's working window into free and busy intervals.
/// </summary>
public sealed class AvailabilityCalculator : IAvailabilityCalculator
{
    public IReadOnlyList<AvailabilityInterval> Calculate(
        DateOnly from, DateOnly to,
        WorkingSchedule schedule,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy)
    {
        // sorted once: SubtractBusy relies on the order, and sorting per day was the hot spot
        var ordered = OrderByStart(busy);

        var result = new List<AvailabilityInterval>();

        foreach (var window in schedule.WindowsBetween(from, to))
            result.AddRange(SubtractBusy(window.Start, window.End, ordered, schedule));

        return result;
    }

    private static (DateTimeOffset Start, DateTimeOffset End)[] OrderByStart(
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy)
    {
        var ordered = busy.ToArray();

        Array.Sort(ordered, static (left, right) => left.Start.CompareTo(right.Start));

        return ordered;
    }

    private static IEnumerable<AvailabilityInterval> SubtractBusy(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        (DateTimeOffset Start, DateTimeOffset End)[] ordered,
        WorkingSchedule schedule)
    {
        // on a normal day every moment shares the window's offset; only a transition needs a lookup
        var sameOffsetAllDay = windowStart.Offset == windowEnd.Offset;

        DateTimeOffset Label(DateTimeOffset moment) =>
            sameOffsetAllDay ? moment.ToOffset(windowStart.Offset) : schedule.ToLocal(moment);

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
