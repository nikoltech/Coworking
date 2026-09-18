using Coworking.Domain.Services.SlotGenerator;
using Coworking.Domain.ValueObjects;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests;

public class SlotGeneratorTests
{
    private static readonly ISlotGenerator Generator = new SlotGenerator();

    private static readonly DateOnly Day = new(2026, 6, 1);

    [Fact]
    public void DayWindow_NotDivisibleBySlot_DropsTheTail()
    {
        // 09:00–18:00 is 21 slots of 25 minutes plus a 15-minute tail
        var slots = Generator.GenerateSlots(Day, Hours("UTC", 9, 18, 25));

        Assert.Equal(21, slots.Count);
        Assert.Equal(new TimeSlot(At(Day, 9, 0), At(Day, 9, 25)), slots[0]);
        Assert.Equal(new TimeSlot(At(Day, 17, 20), At(Day, 17, 45)), slots[^1]);
    }

    [Fact]
    public void WindowShorterThanASlot_ProducesNothing()
    {
        // a 60-minute window cannot hold a 90-minute slot
        var slots = Generator.GenerateSlots(Day, Hours("UTC", 9, 10, 90));

        Assert.Empty(slots);
    }

    [Fact]
    public void NightWindow_RunsIntoTheNextDay()
    {
        var slots = Generator.GenerateSlots(Day, Hours("UTC", 22, 6, 60));

        Assert.Equal(8, slots.Count);
        Assert.Equal(new TimeSlot(At(Day, 22, 0), At(Day, 23, 0)), slots[0]);
        Assert.Equal(new TimeSlot(At(Day.AddDays(1), 5, 0), At(Day.AddDays(1), 6, 0)), slots[^1]);
    }

    // a DST day is 23 or 25 hours long, so it simply holds fewer or more whole slots
    [Theory]
    [InlineData(2030, 3, 31, 46)]
    [InlineData(2030, 10, 27, 50)]
    [InlineData(2030, 6, 1, 48)]
    public void NonStopDay_HoldsAsManySlotsAsItReallyLasts(int year, int month, int day, int expected)
    {
        var slots = Generator.GenerateSlots(new DateOnly(year, month, day), NonStop("Europe/Kyiv", 30));

        Assert.Equal(expected, slots.Count);
    }

    [Fact]
    public void TransitionDay_SlotsAreContiguousAndCarryTheirOwnOffset()
    {
        var springForward = new DateOnly(2030, 3, 31);

        var slots = Generator.GenerateSlots(springForward, NonStop("Europe/Kyiv", 30));

        Assert.Equal(TimeSpan.FromHours(2), slots[0].Start.Offset);
        Assert.Equal(TimeSpan.FromHours(3), slots[^1].End.Offset);
        Assert.Equal(springForward.AddDays(1).ToDateTime(TimeOnly.MinValue), slots[^1].End.DateTime);

        for (var i = 1; i < slots.Count; i++)
            Assert.Equal(slots[i - 1].End, slots[i].Start);
    }

    private static WorkingSchedule Hours(string timeZoneId, int open, int close, int slotMinutes) =>
        WorkingSchedule.For(CoworkingEntity.CreateWithHours(
            "Test", timeZoneId, SlotSize.From(slotMinutes), new TimeOnly(open, 0), new TimeOnly(close, 0)));

    private static WorkingSchedule NonStop(string timeZoneId, int slotMinutes) =>
        WorkingSchedule.For(CoworkingEntity.CreateNonStop("Test", timeZoneId, SlotSize.From(slotMinutes)));

    private static DateTimeOffset At(DateOnly day, int hour, int minute) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);
}
