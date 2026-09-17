using Coworking.Domain.Exceptions;
using Coworking.Domain.ValueObjects;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests.Bookings;

public class WorkingScheduleTests
{
    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Eighteen = new(18, 0);
    private static readonly TimeOnly TwentyTwo = new(22, 0);
    private static readonly TimeOnly Six = new(6, 0);

    private static readonly WorkingSchedule DayHours = Hours(Nine, Eighteen);
    private static readonly WorkingSchedule NightHours = Hours(TwentyTwo, Six);
    private static readonly WorkingSchedule NonStop = WorkingSchedule.For(Coworking(isNonStop: true));

    // factory

    [Fact]
    public void For_UnknownTimeZone_ThrowsDomainExceptionWithTheCause()
    {
        var coworking = Coworking(isNonStop: true);
        coworking.TimeZoneId = "Mars/Olympus_Mons";

        var ex = Assert.Throws<DomainException>(() => WorkingSchedule.For(coworking));

        Assert.IsType<TimeZoneNotFoundException>(ex.InnerException);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(9, null)]
    [InlineData(null, 18)]
    public void For_MissingHours_Throws(int? openHour, int? closeHour)
    {
        var coworking = Coworking(open: ToTime(openHour), close: ToTime(closeHour));

        Assert.Throws<DomainException>(() => WorkingSchedule.For(coworking));
    }

    [Fact]
    public void For_EqualHoursWithoutNonStopFlag_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => WorkingSchedule.For(Coworking(open: Nine, close: Nine)));

        Assert.Contains("non-stop", ex.Message);
    }

    [Fact]
    public void For_NonStop_IgnoresHours()
    {
        var schedule = WorkingSchedule.For(Coworking(open: Nine, close: Nine, isNonStop: true));

        Assert.True(schedule.IsNonStop);
    }

    // working hours: order

    [Fact]
    public void StartAfterEnd_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            DayHours.EnsureWithinWorkingHours(At(1, 12, 0), At(1, 11, 0)));

        Assert.Contains("earlier than end", ex.Message);
    }

    [Fact]
    public void StartEqualToEnd_Throws()
    {
        Assert.Throws<DomainException>(() =>
            DayHours.EnsureWithinWorkingHours(At(1, 12, 0), At(1, 12, 0)));
    }

    [Fact]
    public void StartAfterEnd_ThrowsEvenForNonStopSchedule()
    {
        Assert.Throws<DomainException>(() =>
            NonStop.EnsureWithinWorkingHours(At(1, 12, 0), At(1, 11, 0)));
    }

    // working hours: day schedule

    [Fact]
    public void DaySchedule_PeriodInsideHours_Passes()
    {
        DayHours.EnsureWithinWorkingHours(At(1, 10, 0), At(1, 12, 0));
    }

    [Fact]
    public void DaySchedule_PeriodExactlyMatchingHours_Passes()
    {
        DayHours.EnsureWithinWorkingHours(At(1, 9, 0), At(1, 18, 0));
    }

    [Fact]
    public void DaySchedule_StartBeforeOpening_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            DayHours.EnsureWithinWorkingHours(At(1, 8, 30), At(1, 10, 0)));

        Assert.Contains("start time is outside", ex.Message);
    }

    [Fact]
    public void DaySchedule_StartAtClosing_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            DayHours.EnsureWithinWorkingHours(At(1, 18, 0), At(2, 10, 0)));

        Assert.Contains("start time is outside", ex.Message);
    }

    [Fact]
    public void DaySchedule_EndAfterClosing_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            DayHours.EnsureWithinWorkingHours(At(1, 17, 0), At(1, 18, 30)));

        Assert.Contains("end time is outside", ex.Message);
    }

    [Fact]
    public void DaySchedule_EndAtOpening_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            DayHours.EnsureWithinWorkingHours(At(1, 17, 0), At(2, 9, 0)));

        Assert.Contains("end time is outside", ex.Message);
    }

    [Fact]
    public void DaySchedule_IsJudgedInTheCoworkingTimeZone()
    {
        var kyiv = Hours(Nine, Eighteen, "Europe/Kyiv");

        // 06:00 UTC is 09:00 in Kyiv in summer
        kyiv.EnsureWithinWorkingHours(At(1, 6, 0), At(1, 7, 0));
    }

    // a booking is one row over the whole period; closed hours inside it are not bookable anyway
    [Fact]
    public void DaySchedule_PeriodSpanningClosedNights_Passes()
    {
        DayHours.EnsureWithinWorkingHours(At(1, 10, 0), At(3, 11, 0));
    }

    // working hours: night schedule

    [Fact]
    public void NightSchedule_PeriodCrossingMidnight_Passes()
    {
        NightHours.EnsureWithinWorkingHours(At(1, 23, 0), At(2, 5, 0));
    }

    [Fact]
    public void NightSchedule_StartInsideDaytimeGap_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            NightHours.EnsureWithinWorkingHours(At(1, 12, 0), At(2, 1, 0)));

        Assert.Contains("start time is outside", ex.Message);
    }

    [Fact]
    public void NightSchedule_PeriodSpanningClosedDaytime_Passes()
    {
        NightHours.EnsureWithinWorkingHours(At(1, 5, 0), At(1, 23, 0));
    }

    // working hours: non-stop

    [Fact]
    public void NonStopSchedule_AnyPeriod_Passes()
    {
        NonStop.EnsureWithinWorkingHours(At(1, 3, 0), At(4, 21, 0));
    }

    // windows

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(13, 0, true)]
    [InlineData(17, 59, true)]
    [InlineData(18, 0, false)]
    [InlineData(8, 59, false)]
    public void DayWindow_ForStart_IncludesOpeningOnly(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, DayHours.WindowForStart(At(1, hour, minute)) is not null);
    }

    [Theory]
    [InlineData(18, 0, true)]
    [InlineData(9, 1, true)]
    [InlineData(9, 0, false)]
    [InlineData(18, 1, false)]
    public void DayWindow_ForEnd_IncludesClosingOnly(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, DayHours.WindowForEnd(At(1, hour, minute)) is not null);
    }

    [Fact]
    public void NightWindow_EarlyMorning_BelongsToThePreviousEvening()
    {
        var window = NightHours.WindowForStart(At(2, 2, 0));

        Assert.Equal(new WorkingWindow(At(1, 22, 0), At(2, 6, 0)), window);
    }

    [Fact]
    public void NonStopWindow_Midnight_StartsTheNewDay()
    {
        Assert.Equal(At(2, 0, 0), NonStop.WindowForStart(At(2, 0, 0))!.Value.Start);
        Assert.Equal(At(2, 0, 0), NonStop.WindowForEnd(At(2, 0, 0))!.Value.End);
    }

    [Fact]
    public void WindowsBetween_NightSchedule_EndsOnTheNextDay()
    {
        var windows = NightHours.WindowsBetween(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));

        Assert.Equal(
            [new WorkingWindow(At(1, 22, 0), At(2, 6, 0)), new WorkingWindow(At(2, 22, 0), At(3, 6, 0))],
            windows);
    }

    [Fact]
    public void QueryBoundaries_CoverTheNightTailOfTheLastDay()
    {
        var (start, end) = DayHours.QueryBoundaries(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));

        Assert.Equal(At(1, 0, 0), start);
        Assert.Equal(At(4, 0, 0), end);
    }

    [Fact]
    public void ToLocal_UsesTheCoworkingOffset()
    {
        var kyiv = Hours(Nine, Eighteen, "Europe/Kyiv");

        Assert.Equal(TimeSpan.FromHours(3), kyiv.ToLocal(At(1, 6, 0)).Offset);
    }

    [Fact]
    public void BillableTime_IsNotImplementedYet()
    {
        Assert.Throws<NotImplementedException>(() => DayHours.BillableTime(At(1, 10, 0), At(1, 11, 0)));
    }

    private static WorkingSchedule Hours(TimeOnly open, TimeOnly close, string timeZoneId = "UTC") =>
        WorkingSchedule.For(Coworking(open, close, timeZoneId: timeZoneId));

    private static CoworkingEntity Coworking(
        TimeOnly? open = null, TimeOnly? close = null, bool isNonStop = false, string timeZoneId = "UTC") =>
        new() { Name = "Test", TimeZoneId = timeZoneId, IsNonStop = isNonStop, OpenTime = open, CloseTime = close };

    private static TimeOnly? ToTime(int? hour) => hour is { } h ? new TimeOnly(h, 0) : null;

    private static DateTimeOffset At(int day, int hour, int minute) =>
        new(2026, 6, day, hour, minute, 0, TimeSpan.Zero);
}
