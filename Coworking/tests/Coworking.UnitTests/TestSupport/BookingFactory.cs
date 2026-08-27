using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests.TestSupport;

/// Builds the Coworking -> Desk -> Booking chain the cancel handler loads.
internal static class BookingFactory
{
    public const string UserEmail = "cancel@example.com";
    public const string UserName = "Cancel Probe";
    public const string DeskName = "D1";
    public const string CoworkingName = "Test Coworking";
    public const string TimeZoneId = "UTC";

    public static readonly DateTimeOffset Start = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset End = Start.AddHours(1);

    public static Booking Seeded(TestAppDbContext db, BookingStatus status)
    {
        var coworking = new CoworkingEntity
        {
            Name = CoworkingName,
            Address = "Test",
            TimeZoneId = TimeZoneId,
            OpenTime = new TimeOnly(0, 0),
            CloseTime = new TimeOnly(0, 0),
            Desks = [new Desk { Name = DeskName, Description = "Test", Coworking = null! }]
        };

        var booking = new Booking
        {
            Desk = coworking.Desks.First(),
            UserName = UserName,
            UserEmail = UserEmail,
            StartTime = Start,
            EndTime = End,
            Status = status
        };

        db.Set<CoworkingEntity>().Add(coworking);
        db.Set<Booking>().Add(booking);
        db.SaveChanges();

        return booking;
    }
}
