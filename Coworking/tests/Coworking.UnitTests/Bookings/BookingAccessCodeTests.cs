using Coworking.Domain.Entities;

namespace Coworking.UnitTests.Bookings;

public class BookingAccessCodeTests
{
    [Fact]
    public void AccessCode_IsUuidV7()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var booking = Booking.Create(1, "Probe", "probe@example.com", start, start.AddHours(1));

        Assert.Equal(7, booking.AccessCode.Version);
    }
}
