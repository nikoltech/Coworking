using Coworking.Domain.Services.Availability;

namespace Coworking.UnitTests;

public class AvailabilityCalculatorTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
    private static readonly TimeZoneInfo Havana = TimeZoneInfo.FindSystemTimeZoneById("America/Havana");

    private static readonly IAvailabilityCalculator Calculator = new AvailabilityCalculator();

    private static readonly DateOnly Day = new(2026, 6, 1);
    private static readonly TimeOnly Open = new(8, 0);
    private static readonly TimeOnly Close = new(20, 0);

    // windows without bookings

    [Fact]
    public void RegularHours_WithoutBookings_ReturnsSingleAvailableInterval()
    {
        var result = Calculate(Day, Day, Open, Close, Utc, []);

        var interval = Assert.Single(result);
        Assert.True(interval.IsAvailable);
        Assert.Equal(At(Day, 8, 0), interval.Start);
        Assert.Equal(At(Day, 20, 0), interval.End);
    }

    [Fact]
    public void NonStopSchedule_CoversWholeDay()
    {
        var midnight = new TimeOnly(0, 0);

        var result = Calculate(Day, Day, midnight, midnight, Utc, []);

        var interval = Assert.Single(result);
        Assert.Equal(At(Day, 0, 0), interval.Start);
        Assert.Equal(At(Day.AddDays(1), 0, 0), interval.End);
    }

    [Fact]
    public void NonStopScheduleAnchoredAwayFromMidnight_EndsOnTheNextDay()
    {
        var result = Calculate(Day, Day, Open, Open, Utc, []);

        var interval = Assert.Single(result);
        Assert.Equal(At(Day, 8, 0), interval.Start);
        Assert.Equal(At(Day.AddDays(1), 8, 0), interval.End);
    }

    [Fact]
    public void NightSchedule_CrossesMidnight()
    {
        var result = Calculate(Day, Day, new TimeOnly(22, 0), new TimeOnly(6, 0), Utc, []);

        var interval = Assert.Single(result);
        Assert.Equal(At(Day, 22, 0), interval.Start);
        Assert.Equal(At(Day.AddDays(1), 6, 0), interval.End);
    }

    [Fact]
    public void MultipleDays_ReturnOneWindowEachInChronologicalOrder()
    {
        var to = Day.AddDays(2);

        var result = Calculate(Day, to, Open, Close, Utc, []);

        Assert.Equal(3, result.Count);
        Assert.Equal([At(Day, 8, 0), At(Day.AddDays(1), 8, 0), At(to, 8, 0)], result.Select(i => i.Start));
    }

    // slot size is a client-side hint — no window is ever trimmed to it

    [Fact]
    public void WindowNotDivisibleBySlot_KeepsItsTail()
    {
        var result = Calculate(Day, Day, Open, new TimeOnly(20, 5), Utc, []);

        var interval = Assert.Single(result);
        Assert.Equal(At(Day, 20, 5), interval.End);
    }

    [Fact]
    public void WindowShorterThanASlot_IsStillReturned()
    {
        var result = Calculate(Day, Day, Open, new TimeOnly(8, 20), Utc, []);

        var interval = Assert.Single(result);
        Assert.True(interval.IsAvailable);
        Assert.Equal(At(Day, 8, 20), interval.End);
    }

    [Fact]
    public void NightScheduleNotDivisibleBySlot_KeepsItsFullLength()
    {
        var result = Calculate(Day, Day, new TimeOnly(22, 0), new TimeOnly(6, 30), Utc, []);

        var interval = Assert.Single(result);
        Assert.Equal(TimeSpan.FromMinutes(510), interval.End - interval.Start);
    }

    // busy subtraction

    [Fact]
    public void BookingInsideWindow_SplitsIntoFreeBusyFree()
    {
        var busy = new[] { (At(Day, 10, 0), At(Day, 12, 0)) };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        Assert.Equal(
            [
                (At(Day, 8, 0), At(Day, 10, 0), true),
                (At(Day, 10, 0), At(Day, 12, 0), false),
                (At(Day, 12, 0), At(Day, 20, 0), true)
            ],
            result.Select(i => (i.Start, i.End, i.IsAvailable)));
    }

    [Fact]
    public void BookingAtWindowStart_ProducesNoLeadingFreeInterval()
    {
        var busy = new[] { (At(Day, 8, 0), At(Day, 9, 0)) };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        Assert.Equal(
            [
                (At(Day, 8, 0), At(Day, 9, 0), false),
                (At(Day, 9, 0), At(Day, 20, 0), true)
            ],
            result.Select(i => (i.Start, i.End, i.IsAvailable)));
    }

    [Fact]
    public void BookingCoveringWholeWindow_LeavesSingleBusyInterval()
    {
        var busy = new[] { (At(Day, 8, 0), At(Day, 20, 0)) };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        var interval = Assert.Single(result);
        Assert.False(interval.IsAvailable);
        Assert.Equal(At(Day, 8, 0), interval.Start);
        Assert.Equal(At(Day, 20, 0), interval.End);
    }

    [Fact]
    public void BookingExtendingBeyondWindow_IsClippedToWindow()
    {
        var busy = new[] { (At(Day, 6, 0), At(Day, 9, 0)) };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        Assert.Equal(At(Day, 8, 0), result[0].Start);
        Assert.False(result[0].IsAvailable);
    }

    [Fact]
    public void BookingOutsideWindow_IsIgnored()
    {
        var busy = new[] { (At(Day, 5, 0), At(Day, 7, 0)) };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        var interval = Assert.Single(result);
        Assert.True(interval.IsAvailable);
    }

    [Fact]
    public void BookingTouchingWindowBorder_IsIgnored()
    {
        var busy = new[] { (At(Day, 6, 0), At(Day, 8, 0)) };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        var interval = Assert.Single(result);
        Assert.True(interval.IsAvailable);
    }

    [Fact]
    public void BookingCrossingMidnight_IsSubtractedFromTheNightWindow()
    {
        var busy = new[] { (At(Day, 23, 0), At(Day.AddDays(1), 1, 0)) };

        var result = Calculate(Day, Day, new TimeOnly(22, 0), new TimeOnly(6, 0), Utc, busy);

        Assert.Equal(
            [
                (At(Day, 22, 0), At(Day, 23, 0), true),
                (At(Day, 23, 0), At(Day.AddDays(1), 1, 0), false),
                (At(Day.AddDays(1), 1, 0), At(Day.AddDays(1), 6, 0), true)
            ],
            result.Select(i => (i.Start, i.End, i.IsAvailable)));
    }

    [Fact]
    public void OverlappingBookings_AreMergedIntoOneBusyInterval()
    {
        var busy = new[]
        {
            (At(Day, 10, 0), At(Day, 12, 0)),
            (At(Day, 11, 0), At(Day, 13, 0))
        };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        Assert.Equal(
            [
                (At(Day, 8, 0), At(Day, 10, 0), true),
                (At(Day, 10, 0), At(Day, 13, 0), false),
                (At(Day, 13, 0), At(Day, 20, 0), true)
            ],
            result.Select(i => (i.Start, i.End, i.IsAvailable)));
    }

    [Fact]
    public void AdjacentBookings_AreMergedIntoOneBusyInterval()
    {
        var busy = new[]
        {
            (At(Day, 12, 0), At(Day, 13, 0)),
            (At(Day, 10, 0), At(Day, 12, 0))
        };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        Assert.Equal(
            [
                (At(Day, 8, 0), At(Day, 10, 0), true),
                (At(Day, 10, 0), At(Day, 13, 0), false),
                (At(Day, 13, 0), At(Day, 20, 0), true)
            ],
            result.Select(i => (i.Start, i.End, i.IsAvailable)));
    }

    [Fact]
    public void NestedBooking_DoesNotExtendMergedBusyInterval()
    {
        var busy = new[]
        {
            (At(Day, 10, 0), At(Day, 14, 0)),
            (At(Day, 11, 0), At(Day, 12, 0))
        };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        Assert.Equal(
            [
                (At(Day, 8, 0), At(Day, 10, 0), true),
                (At(Day, 10, 0), At(Day, 14, 0), false),
                (At(Day, 14, 0), At(Day, 20, 0), true)
            ],
            result.Select(i => (i.Start, i.End, i.IsAvailable)));
    }

    [Fact]
    public void BookingOnOneDay_DoesNotAffectOtherDays()
    {
        var busy = new[] { (At(Day, 10, 0), At(Day, 12, 0)) };

        var result = Calculate(Day, Day.AddDays(1), Open, Close, Utc, busy);

        Assert.Equal(3, result.Count(i => i.Start < At(Day.AddDays(1), 0, 0)));
        Assert.True(result[^1].IsAvailable);
        Assert.Equal(At(Day.AddDays(1), 8, 0), result[^1].Start);
    }

    [Fact]
    public void ResultingIntervals_AreContiguousWithinEachWindow()
    {
        var busy = new[]
        {
            (At(Day, 9, 0), At(Day, 10, 0)),
            (At(Day, 15, 30), At(Day, 16, 0))
        };

        var result = Calculate(Day, Day, Open, Close, Utc, busy);

        for (var i = 1; i < result.Count; i++)
            Assert.Equal(result[i - 1].End, result[i].Start);
    }

    // DST — shifts the real length of a day, never breaks it apart

    [Theory]
    [InlineData(2026, 3, 29, 23)]
    [InlineData(2026, 10, 25, 25)]
    [InlineData(2026, 6, 1, 24)]
    public void NonStopSchedule_KeepsDayContinuousAcrossTransitions(int year, int month, int day, int expectedHours)
    {
        var date = new DateOnly(year, month, day);
        var midnight = new TimeOnly(0, 0);

        var intervals = Calculate(date, date, midnight, midnight, Kyiv, []);

        var interval = Assert.Single(intervals);
        Assert.True(interval.IsAvailable);
        Assert.Equal(TimeSpan.FromHours(expectedHours), interval.End - interval.Start);
    }

    [Theory]
    [InlineData(2026, 3, 29, 46)]
    [InlineData(2026, 10, 25, 50)]
    public void TransitionDay_ExpandsIntoUniformSlotsWithoutRemainder(int year, int month, int day, int expectedSlots)
    {
        var date = new DateOnly(year, month, day);
        var midnight = new TimeOnly(0, 0);
        var slotLength = TimeSpan.FromMinutes(30);

        var intervals = Calculate(date, date, midnight, midnight, Kyiv, []);
        var slots = Expand(intervals, slotLength);

        Assert.Equal(expectedSlots, slots.Count);
        Assert.Equal(intervals[0].End, slots[^1].End);
    }

    [Fact]
    public void TransitionDay_CarriesTheOffsetOfEachBoundary()
    {
        var date = new DateOnly(2026, 3, 29);
        var midnight = new TimeOnly(0, 0);

        var interval = Assert.Single(Calculate(date, date, midnight, midnight, Kyiv, []));

        Assert.Equal(TimeSpan.FromHours(2), interval.Start.Offset);
        Assert.Equal(TimeSpan.FromHours(3), interval.End.Offset);
    }

    [Fact]
    public void NightScheduleOverATransition_KeepsItsRealLength()
    {
        var date = new DateOnly(2026, 3, 28);

        var intervals = Calculate(date, date, new TimeOnly(22, 0), new TimeOnly(6, 0), Kyiv, []);

        var interval = Assert.Single(intervals);
        Assert.Equal(TimeSpan.FromHours(7), interval.End - interval.Start);
    }

    [Fact]
    public void DayWithoutTransition_UsesLocalOffsetForWholeWindow()
    {
        var date = new DateOnly(2026, 6, 1);

        var intervals = Calculate(date, date, Open, Close, Kyiv, []);

        var interval = Assert.Single(intervals);
        Assert.Equal(Kyiv.GetUtcOffset(date.ToDateTime(Open)), interval.Start.Offset);
        Assert.Equal(TimeSpan.FromHours(12), interval.End - interval.Start);
    }

    [Fact]
    public void SpringForwardWithBooking_SubtractsInsideTheWindow()
    {
        var date = new DateOnly(2026, 3, 29);
        var midnight = new TimeOnly(0, 0);
        var summerOffset = TimeSpan.FromHours(3);
        var busy = new[]
        {
            (new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), summerOffset),
             new DateTimeOffset(date.ToDateTime(new TimeOnly(11, 0)), summerOffset))
        };

        var intervals = Calculate(date, date, midnight, midnight, Kyiv, busy);

        var booked = Assert.Single(intervals, i => !i.IsAvailable);
        Assert.Equal(busy[0].Item1, booked.Start);
        Assert.Equal(busy[0].Item2, booked.End);
    }

    // equality compares instants, so only an explicit offset check catches a wrong label
    [Fact]
    public void BookingAfterATransition_CarriesTheOffsetOfItsOwnInstant()
    {
        var date = new DateOnly(2026, 3, 29);
        var midnight = new TimeOnly(0, 0);
        var summerOffset = TimeSpan.FromHours(3);
        var busy = new[]
        {
            (new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), summerOffset),
             new DateTimeOffset(date.ToDateTime(new TimeOnly(11, 0)), summerOffset))
        };

        var intervals = Calculate(date, date, midnight, midnight, Kyiv, busy);

        // the transition is at 03:00, so a 10:00 booking sits entirely on summer time
        var booked = Assert.Single(intervals, i => !i.IsAvailable);
        Assert.Equal(summerOffset, booked.Start.Offset);
        Assert.Equal(summerOffset, booked.End.Offset);
    }

    // Havana switches at 00:00, so local midnight does not exist on the transition day.
    [Fact]
    public void ZoneTransitioningAtMidnight_ResolvesWithoutThrowing()
    {
        var date = new DateOnly(2027, 3, 14);
        var midnight = new TimeOnly(0, 0);

        var intervals = Calculate(date, date, midnight, midnight, Havana, []);

        var interval = Assert.Single(intervals);
        Assert.Equal(TimeSpan.FromHours(23), interval.End - interval.Start);
    }

    // helpers

    private static IReadOnlyList<AvailabilityInterval> Calculate(
        DateOnly from, DateOnly to,
        TimeOnly openTime, TimeOnly closeTime,
        TimeZoneInfo timeZone,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy) =>
        Calculator.Calculate(from, to, openTime, closeTime, timeZone, busy);

    private static DateTimeOffset At(DateOnly date, int hour, int minute) =>
        new(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);

    private static List<(DateTimeOffset Start, DateTimeOffset End)> Expand(
        IReadOnlyList<AvailabilityInterval> intervals, TimeSpan slotLength)
    {
        var slots = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var interval in intervals)
        {
            var current = interval.Start;

            while (current + slotLength <= interval.End)
            {
                slots.Add((current, current + slotLength));
                current += slotLength;
            }
        }

        return slots;
    }
}
