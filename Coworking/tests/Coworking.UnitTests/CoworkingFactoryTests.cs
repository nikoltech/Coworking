using Coworking.Domain.ValueObjects;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests;

/// <summary>
/// The factories must not build a coworking WorkingSchedule would then reject. Bad arguments stay
/// ArgumentException: the caller got them wrong, and the parameter name says which one.
/// </summary>
public class CoworkingFactoryTests
{
    private static readonly TimeOnly Nine = new(9, 0);
    private static readonly TimeOnly Eighteen = new(18, 0);

    [Fact]
    public void CreateWithHours_KeepsTheHours()
    {
        var coworking = CoworkingEntity.CreateWithHours("Hub", "Europe/Kyiv", SlotSize.ThirtyMinutes, Nine, Eighteen);

        Assert.False(coworking.IsNonStop);
        Assert.Equal((Nine, Eighteen), (coworking.OpenTime, coworking.CloseTime));
        Assert.NotNull(WorkingSchedule.For(coworking));
    }

    [Fact]
    public void CreateNonStop_LeavesNoHours()
    {
        var coworking = CoworkingEntity.CreateNonStop("Hub", "Europe/Kyiv", SlotSize.ThirtyMinutes);

        Assert.True(coworking.IsNonStop);
        Assert.Equal((null, null), (coworking.OpenTime, coworking.CloseTime));
        Assert.True(WorkingSchedule.For(coworking).IsNonStop);
    }

    [Fact]
    public void CreateWithHours_EqualHours_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CoworkingEntity.CreateWithHours("Hub", "Europe/Kyiv", SlotSize.ThirtyMinutes, Nine, Nine));

        Assert.Equal("closeTime", ex.ParamName);
    }

    [Fact]
    public void UnknownTimeZone_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CoworkingEntity.CreateNonStop("Hub", "Mars/Olympus_Mons", SlotSize.ThirtyMinutes));

        Assert.Equal("timeZoneId", ex.ParamName);
        Assert.IsType<TimeZoneNotFoundException>(ex.InnerException);
    }

    // a Windows id resolves differently per host, so stored data would stop being portable
    [Fact]
    public void WindowsTimeZoneId_IsRejectedWithTheIanaName()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CoworkingEntity.CreateNonStop("Hub", "FLE Standard Time", SlotSize.ThirtyMinutes));

        Assert.Contains("not an IANA id", ex.Message);
        Assert.Contains("Europe/", ex.Message);
    }

    [Theory]
    [InlineData("", "Europe/Kyiv", "name")]
    [InlineData(" ", "Europe/Kyiv", "name")]
    [InlineData("Hub", "", "timeZoneId")]
    [InlineData("Hub", " ", "timeZoneId")]
    public void BlankNameOrTimeZone_NamesTheParameter(string name, string timeZoneId, string expectedParameter)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CoworkingEntity.CreateNonStop(name, timeZoneId, SlotSize.ThirtyMinutes));

        Assert.Equal(expectedParameter, ex.ParamName);
    }
}
