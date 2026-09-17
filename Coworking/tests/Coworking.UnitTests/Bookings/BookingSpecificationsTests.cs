using Coworking.Domain.Exceptions;
using Coworking.Domain.Specifications;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests.Bookings;

public class BookingSpecificationsTests
{
    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Eighteen = new(18, 0);
    private static readonly TimeOnly TwentyTwo = new(22, 0);
    private static readonly TimeOnly Six = new(6, 0);

    // period validation: order

    [Fact]
    public void StartAfterEnd_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Validate(At(1, 12, 0), At(1, 11, 0), Nine, Eighteen));

        Assert.Contains("earlier than end", ex.Message);
    }

    [Fact]
    public void StartEqualToEnd_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Validate(At(1, 12, 0), At(1, 12, 0), Nine, Eighteen));
    }

    [Fact]
    public void StartAfterEnd_ThrowsEvenForNonStopSchedule()
    {
        Assert.Throws<DomainException>(() =>
            Validate(At(1, 12, 0), At(1, 11, 0), Nine, Nine));
    }

    // period validation: day schedule

    [Fact]
    public void DaySchedule_PeriodInsideHours_Passes()
    {
        Validate(At(1, 10, 0), At(1, 12, 0), Nine, Eighteen);
    }

    [Fact]
    public void DaySchedule_PeriodExactlyMatchingHours_Passes()
    {
        Validate(At(1, 9, 0), At(1, 18, 0), Nine, Eighteen);
    }

    [Fact]
    public void DaySchedule_StartBeforeOpening_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Validate(At(1, 8, 30), At(1, 10, 0), Nine, Eighteen));

        Assert.Contains("start time is outside", ex.Message);
    }

    [Fact]
    public void DaySchedule_EndAfterClosing_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Validate(At(1, 17, 0), At(1, 18, 30), Nine, Eighteen));

        Assert.Contains("end time is outside", ex.Message);
    }

    [Fact]
    public void DaySchedule_UsesClockTimeOfTheGivenOffset()
    {
        // 09:00 at +03:00 is 06:00 UTC; the caller converts to coworking-local time beforehand
        var start = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.FromHours(3));

        Validate(start, start.AddHours(1), Nine, Eighteen);
    }

    // a booking is one row over the whole period; closed hours inside it are not bookable anyway
    [Fact]
    public void DaySchedule_PeriodSpanningClosedNights_Passes()
    {
        Validate(At(1, 10, 0), At(3, 11, 0), Nine, Eighteen);
    }

    // period validation: overnight schedule

    [Fact]
    public void OvernightSchedule_PeriodCrossingMidnight_Passes()
    {
        Validate(At(1, 23, 0), At(2, 5, 0), TwentyTwo, Six);
    }

    [Fact]
    public void OvernightSchedule_StartInsideDaytimeGap_Throws()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Validate(At(1, 12, 0), At(2, 1, 0), TwentyTwo, Six));

        Assert.Contains("start time is outside", ex.Message);
    }

    [Fact]
    public void OvernightSchedule_PeriodSpanningClosedDaytime_Passes()
    {
        Validate(At(1, 5, 0), At(1, 23, 0), TwentyTwo, Six);
    }

    // period validation: non-stop schedule

    [Fact]
    public void NonStopSchedule_AnyPeriod_Passes()
    {
        Validate(At(1, 3, 0), At(4, 21, 0), Nine, Nine);
    }

    // working window

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(18, 0, true)]
    [InlineData(13, 0, true)]
    [InlineData(8, 59, false)]
    [InlineData(18, 1, false)]
    [InlineData(0, 0, false)]
    public void DayWindow_IncludesBothBorders(int hour, int minute, bool expected)
    {
        Assert.Equal(expected,
            BookingSpecifications.IsWithinWorkingWindow(new TimeOnly(hour, minute), Nine, Eighteen));
    }

    [Theory]
    [InlineData(22, 0, true)]
    [InlineData(23, 59, true)]
    [InlineData(0, 0, true)]
    [InlineData(6, 0, true)]
    [InlineData(6, 1, false)]
    [InlineData(12, 0, false)]
    [InlineData(21, 59, false)]
    public void OvernightWindow_WrapsAroundMidnight(int hour, int minute, bool expected)
    {
        Assert.Equal(expected,
            BookingSpecifications.IsWithinWorkingWindow(new TimeOnly(hour, minute), TwentyTwo, Six));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(8, 59)]
    [InlineData(23, 59)]
    public void EqualOpenAndClose_AcceptsAnyTime(int hour, int minute)
    {
        Assert.True(BookingSpecifications.IsWithinWorkingWindow(new TimeOnly(hour, minute), Nine, Nine));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(9, 18, false)]
    [InlineData(22, 6, false)]
    public void IsNonStopWorkingHours_WhenOpenEqualsClose(int openHour, int closeHour, bool expected)
    {
        var coworking = CoworkingWith(new TimeOnly(openHour, 0), new TimeOnly(closeHour, 0));

        Assert.Equal(expected, BookingSpecifications.IsNonStopWorkingHours(coworking));
    }

    private static void Validate(DateTimeOffset start, DateTimeOffset end, TimeOnly open, TimeOnly close) =>
        BookingSpecifications.ValidateAccessPeriod(start, end, CoworkingWith(open, close));

    private static CoworkingEntity CoworkingWith(TimeOnly open, TimeOnly close) =>
        new() { Name = "Test", TimeZoneId = "UTC", OpenTime = open, CloseTime = close };

    private static DateTimeOffset At(int day, int hour, int minute) =>
        new(2026, 6, day, hour, minute, 0, TimeSpan.Zero);
}
