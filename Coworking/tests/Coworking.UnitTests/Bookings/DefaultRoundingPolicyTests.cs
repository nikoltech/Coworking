using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.ValueObjects;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests.Bookings;

/// <summary>
/// The slot grid starts at each window's opening and steps in real time, whatever the offset.
/// Results are compared as local labels: equal instants with a wrong offset still fail.
/// </summary>
public class DefaultRoundingPolicyTests
{
    private static readonly IBookingRoundingPolicy Policy = new DefaultRoundingPolicy();

    private static readonly DateOnly Day = new(2026, 6, 1);
    private static readonly TimeSpan Kyiv = TimeSpan.FromHours(3);
    private static readonly TimeSpan Kathmandu = new(5, 45, 0);
    private static readonly TimeSpan Kolkata = new(5, 30, 0);

    // whole-hour offset

    [Fact]
    public void WholeHourOffset_ExpandsToSlotBorders()
    {
        var schedule = Hours("Europe/Kyiv", 9, 0, 18, 0, 30);

        AssertRounded(schedule,
            (Local(Day, 9, 10, Kyiv), Local(Day, 9, 50, Kyiv)),
            (Local(Day, 9, 0, Kyiv), Local(Day, 10, 0, Kyiv)));
    }

    // fractional offsets: the grid follows local time, not UTC

    [Fact]
    public void QuarterHourOffset_KeepsLocalSlotBorders()
    {
        var schedule = Hours("Asia/Kathmandu", 9, 0, 18, 0, 30);

        AssertRounded(schedule,
            (Local(Day, 9, 0, Kathmandu), Local(Day, 10, 0, Kathmandu)),
            (Local(Day, 9, 0, Kathmandu), Local(Day, 10, 0, Kathmandu)));
    }

    [Theory]
    [InlineData(9, 10)]
    [InlineData(9, 20)]
    public void QuarterHourOffset_FloorsToLocalSlotBorder(int hour, int minute)
    {
        var schedule = Hours("Asia/Kathmandu", 9, 0, 18, 0, 30);

        AssertRounded(schedule,
            (Local(Day, hour, minute, Kathmandu), Local(Day, 10, 5, Kathmandu)),
            (Local(Day, 9, 0, Kathmandu), Local(Day, 10, 30, Kathmandu)));
    }

    [Fact]
    public void HalfHourOffset_WithHourSlot_KeepsLocalSlotBorders()
    {
        var schedule = Hours("Asia/Kolkata", 9, 0, 18, 0, 60);

        AssertRounded(schedule,
            (Local(Day, 9, 40, Kolkata), Local(Day, 10, 10, Kolkata)),
            (Local(Day, 9, 0, Kolkata), Local(Day, 11, 0, Kolkata)));
    }

    [Fact]
    public void RequestInForeignOffset_IsLabelledWithCoworkingOffset()
    {
        var schedule = Hours("Asia/Kathmandu", 9, 0, 18, 0, 30);

        // 03:25 UTC is 09:10 in Kathmandu
        AssertRounded(schedule,
            (Local(Day, 3, 25, TimeSpan.Zero), Local(Day, 4, 15, TimeSpan.Zero)),
            (Local(Day, 9, 0, Kathmandu), Local(Day, 10, 0, Kathmandu)));
    }

    // anchor and clipping

    [Fact]
    public void GridStartsAtOpening()
    {
        var schedule = Hours("Asia/Kolkata", 9, 30, 18, 0, 60);

        AssertRounded(schedule,
            (Local(Day, 10, 0, Kolkata), Local(Day, 10, 40, Kolkata)),
            (Local(Day, 9, 30, Kolkata), Local(Day, 11, 30, Kolkata)));
    }

    [Fact]
    public void SlotNotDividingTheWindow_EndIsClippedAtClosing()
    {
        // 25-minute grid from 09:00: … 17:20, 17:45, 18:10
        var schedule = Hours("UTC", 9, 0, 18, 0, 25);

        AssertRounded(schedule,
            (Local(Day, 17, 30, TimeSpan.Zero), Local(Day, 17, 50, TimeSpan.Zero)),
            (Local(Day, 17, 20, TimeSpan.Zero), Local(Day, 18, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void NightWindow_UsesTheGridOfThePreviousEvening()
    {
        var schedule = Hours("UTC", 22, 0, 6, 0, 30);
        var nextDay = Day.AddDays(1);

        AssertRounded(schedule,
            (Local(nextDay, 2, 10, TimeSpan.Zero), Local(nextDay, 5, 50, TimeSpan.Zero)),
            (Local(nextDay, 2, 0, TimeSpan.Zero), Local(nextDay, 6, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void MomentsOutsideWorkingHours_AreLeftForValidation()
    {
        var schedule = Hours("UTC", 9, 0, 18, 0, 30);

        AssertRounded(schedule,
            (Local(Day, 8, 50, TimeSpan.Zero), Local(Day, 18, 20, TimeSpan.Zero)),
            (Local(Day, 8, 50, TimeSpan.Zero), Local(Day, 18, 20, TimeSpan.Zero)));
    }

    // DST: steps are real time, labels follow the offset of each instant

    [Fact]
    public void SpringForward_LabelsEachBorderWithItsOwnOffset()
    {
        var transition = new DateOnly(2030, 3, 31);
        var schedule = NonStop("Europe/Kyiv", 60);

        AssertRounded(schedule,
            (Local(transition, 2, 30, TimeSpan.FromHours(2)), Local(transition, 4, 10, TimeSpan.FromHours(3))),
            (Local(transition, 2, 0, TimeSpan.FromHours(2)), Local(transition, 5, 0, TimeSpan.FromHours(3))));
    }

    [Fact]
    public void FallBack_RepeatedHourIsASeparateGridPoint()
    {
        var transition = new DateOnly(2030, 10, 27);
        var schedule = NonStop("Europe/Kyiv", 30);

        // the second 03:00, on winter time
        AssertRounded(schedule,
            (Local(transition, 3, 10, TimeSpan.FromHours(2)), Local(transition, 4, 0, TimeSpan.FromHours(2))),
            (Local(transition, 3, 0, TimeSpan.FromHours(2)), Local(transition, 4, 0, TimeSpan.FromHours(2))));
    }

    [Fact]
    public void FallBack_PeriodOverTheRepeatedHour_KeepsItsRealLength()
    {
        var transition = new DateOnly(2030, 10, 27);
        var schedule = NonStop("Europe/Kyiv", 30);

        var (start, end) = Policy.RoundInterval(
            Local(transition, 2, 30, TimeSpan.FromHours(3)),
            Local(transition, 4, 0, TimeSpan.FromHours(2)),
            schedule);

        Assert.Equal(TimeSpan.FromHours(2.5), end - start);
    }

    private static void AssertRounded(
        WorkingSchedule schedule,
        (DateTimeOffset Start, DateTimeOffset End) requested,
        (DateTimeOffset Start, DateTimeOffset End) expected)
    {
        var (start, end) = Policy.RoundInterval(requested.Start, requested.End, schedule);

        Assert.Equal((expected.Start.DateTime, expected.Start.Offset), (start.DateTime, start.Offset));
        Assert.Equal((expected.End.DateTime, expected.End.Offset), (end.DateTime, end.Offset));
    }

    private static WorkingSchedule Hours(
        string timeZoneId, int openHour, int openMinute, int closeHour, int closeMinute, int slotMinutes) =>
        WorkingSchedule.For(new CoworkingEntity
        {
            Name = "Test",
            TimeZoneId = timeZoneId,
            SlotSize = SlotSize.From(slotMinutes),
            OpenTime = new TimeOnly(openHour, openMinute),
            CloseTime = new TimeOnly(closeHour, closeMinute)
        });

    private static WorkingSchedule NonStop(string timeZoneId, int slotMinutes) =>
        WorkingSchedule.For(new CoworkingEntity
        {
            Name = "Test",
            TimeZoneId = timeZoneId,
            SlotSize = SlotSize.From(slotMinutes),
            IsNonStop = true
        });

    private static DateTimeOffset Local(DateOnly day, int hour, int minute, TimeSpan offset) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), offset);
}
