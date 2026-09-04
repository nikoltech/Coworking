using Coworking.Domain.Entities;

namespace Coworking.UnitTests.Bookings;

/// <summary>
/// The cancel validator only accepts version 7, so every way of building a booking has to
/// produce one. DevDataSeeder set its own Guid.NewGuid() and quietly seeded codes that could
/// no longer be cancelled.
/// </summary>
public class BookingAccessCodeTests
{
    [Fact]
    public void Create_ProducesVersion7()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var booking = Booking.Create(1, "Probe", "probe@example.com", start, start.AddHours(1));

        Assert.Equal(7, booking.AccessCode.Version);
    }

    [Fact]
    public void ObjectInitializer_ProducesVersion7()
    {
        var booking = new Booking
        {
            UserName = "Probe",
            UserEmail = "probe@example.com",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            Desk = null!
        };

        Assert.Equal(7, booking.AccessCode.Version);
    }
}
