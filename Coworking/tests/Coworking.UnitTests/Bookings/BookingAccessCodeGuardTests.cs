using Coworking.Domain.Entities;

namespace Coworking.UnitTests.Bookings;

/// <summary>
/// Cancellation only accepts version 7, so the entity refuses any other version outright.
/// DevDataSeeder once set its own Guid.NewGuid() and quietly seeded uncancellable bookings.
/// </summary>
public class BookingAccessCodeGuardTests
{
    [Fact]
    public void Version7_IsAccepted()
    {
        var code = Guid.CreateVersion7();

        var booking = new Booking { AccessCode = code, Desk = null! };

        Assert.Equal(code, booking.AccessCode);
    }

    [Theory]
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c3301")]   // v4
    [InlineData("00000000-0000-0000-0000-000000000000")]   // empty
    public void OtherVersions_AreRejected(string value)
    {
        var booking = new Booking { Desk = null! };

        var ex = Assert.Throws<ArgumentException>(() => booking.AccessCode = Guid.Parse(value));

        Assert.Equal("value", ex.ParamName);
    }
}
