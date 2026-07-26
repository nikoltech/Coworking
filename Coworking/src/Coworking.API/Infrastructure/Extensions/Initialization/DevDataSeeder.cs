using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Domain.ValueObjects;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.API.Infrastructure.Extensions.Initialization;

// ============================================================================
// DEV SEED (remove me)
// Throwaway helper for filling/wiping the DB during local experiments.
// To remove the feature: delete this file, the DEV SEED block in
// AppInitializationExtensions, and the General:SeedData/ResetData flags.
// ============================================================================
internal static class DevDataSeeder
{
    /// <summary>
    /// Inserts a sample data graph. No-op if any coworking already exists (idempotent).
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Set<Coworking.Domain.Entities.Coworking>().AnyAsync(ct))
            return;

        var coworkings = BuildSeedGraph();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.Set<Coworking.Domain.Entities.Coworking>().AddRangeAsync(coworkings, ct);
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }

    /// <summary>
    /// Wipes every mapped table (domain + MassTransit outbox/inbox) and resets identity.
    /// Migration history is preserved. PostgreSQL-specific.
    /// </summary>
    public static async Task ResetAsync(AppDbContext db, CancellationToken ct)
    {
        var tables = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(name => string.IsNullOrWhiteSpace(name) is false)
            .Distinct()
            .Select(name => $"\"{name}\"")
            .ToList();

        if (tables.Count == 0)
            return;

        var sql = $"TRUNCATE TABLE {string.Join(", ", tables)} RESTART IDENTITY CASCADE";

        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    /** seed data **********************/

    private static List<Coworking.Domain.Entities.Coworking> BuildSeedGraph()
    {
        // BookingTimeInterceptor rounds this to whole minutes on save
        var now = DateTimeOffset.UtcNow;

        var central = new Coworking.Domain.Entities.Coworking
        {
            Name = "Central Hub",
            Address = "12 Khreshchatyk St, Kyiv",
            TimeZoneId = "Europe/Kyiv",
            SlotSize = SlotSize.ThirtyMinutes,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(22, 0),
            Desks =
            [
                new Desk { Name = "A1", Description = "Window seat", Coworking = null! },
                new Desk { Name = "A2", Description = "Standing desk", Coworking = null! },
                new Desk { Name = "A3", Description = "Quiet zone", Coworking = null! }
            ]
        };

        var riverside = new Coworking.Domain.Entities.Coworking
        {
            Name = "Riverside Space",
            Address = "5 Thames Walk, London",
            TimeZoneId = "Europe/London",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(18, 0),
            Desks =
            [
                new Desk { Name = "B1", Description = "Corner desk", Coworking = null! },
                new Desk { Name = "B2", Description = "Dual monitor", Coworking = null! }
            ]
        };

        // 24/7: OpenTime == CloseTime marks non-stop working hours (see BookingSpecifications.IsNonStopWorkingHours).
        var nightOwl = new Coworking.Domain.Entities.Coworking
        {
            Name = "Night Owl 24/7",
            Address = "8 Lukyanivska St, Kyiv",
            TimeZoneId = "Europe/Kyiv",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(0, 0),
            CloseTime = new TimeOnly(0, 0),
            Desks =
            [
                new Desk { Name = "C1", Description = "Pod #1", Coworking = null! },
                new Desk { Name = "C2", Description = "Pod #2", Coworking = null! }
            ]
        };

        // Window crosses midnight and is not a whole number of slots (8h30 / 60min).
        var nightShift = new Coworking.Domain.Entities.Coworking
        {
            Name = "Night Shift",
            Address = "3 Politekhnichna St, Kyiv",
            TimeZoneId = "Europe/Kyiv",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(22, 0),
            CloseTime = new TimeOnly(6, 30),
            Desks =
            [
                new Desk { Name = "D1", Description = "Late shift", Coworking = null! },
                new Desk { Name = "D2", Description = "Late shift", Coworking = null! }
            ]
        };

        // 24/7 anchored away from midnight — its window ends on the next calendar day.
        var roundClock = new Coworking.Domain.Entities.Coworking
        {
            Name = "Round Clock",
            Address = "17 Vasylkivska St, Kyiv",
            TimeZoneId = "Europe/Kyiv",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(8, 0),
            Desks =
            [
                new Desk { Name = "E1", Description = "Always open", Coworking = null! }
            ]
        };

        // Havana switches DST at 00:00, so local midnight does not exist on transition days.
        var havana = new Coworking.Domain.Entities.Coworking
        {
            Name = "Havana Patio",
            Address = "220 Calle Obispo, Havana",
            TimeZoneId = "America/Havana",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(0, 0),
            CloseTime = new TimeOnly(0, 0),
            Desks =
            [
                new Desk { Name = "F1", Description = "Courtyard", Coworking = null! }
            ]
        };

        var centralDesk = central.Desks.First();
        centralDesk.Bookings =
        [
            BuildBooking("Anna Koval",     "anna@example.com", now.AddHours(2),              now.AddHours(3),              BookingStatus.Confirmed,      "Europe/Kyiv"),
            BuildBooking("Ivan Petrenko",  "ivan@example.com", now.AddDays(1),               now.AddDays(1).AddHours(2),   BookingStatus.PendingPayment, "Europe/Kyiv"),
            BuildBooking("Olha Sydorenko", "olha@example.com", now.AddDays(-2),              now.AddDays(-2).AddHours(1),  BookingStatus.Expired,        "Europe/Kyiv")
        ];

        var riversideDesk = riverside.Desks.First();
        riversideDesk.Bookings =
        [
            BuildBooking("John Smith", "john@example.com", now.AddHours(5),              now.AddHours(6),              BookingStatus.Confirmed,  "Europe/London"),
            BuildBooking("Emma Brown", "emma@example.com", now.AddDays(2),               now.AddDays(2).AddHours(1),   BookingStatus.Cancelled,  "Europe/London")
        ];

        // Night-time booking to exercise the 24/7 window (off-hours allowed only because non-stop).
        var nightOwlDesk = nightOwl.Desks.First();
        nightOwlDesk.Bookings =
        [
            BuildBooking("Max Nadia", "max@example.com", now.AddDays(1).AddHours(3), now.AddDays(1).AddHours(5), BookingStatus.Confirmed, "Europe/Kyiv")
        ];

        // Midnight-crossing booking (23:00 → 01:00 next day). Valid only under non-stop hours.
        var tomorrowMidnight = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1), TimeSpan.Zero);
        var nightOwlDesk2 = nightOwl.Desks.Last();
        nightOwlDesk2.Bookings =
        [
            BuildBooking("Lea Wong", "lea@example.com", tomorrowMidnight.AddHours(-1), tomorrowMidnight.AddHours(1), BookingStatus.Confirmed, "Asia/Singapore")
        ];

        // Both land past midnight — in the tail of the window that opens the day before.
        var kyiv = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
        var tomorrowInKyiv = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, kyiv).DateTime).AddDays(1);

        var nightShiftDesk = nightShift.Desks.First();
        nightShiftDesk.Bookings =
        [
            BuildBooking("Taras Bondar", "taras@example.com",
                LocalTime(tomorrowInKyiv, new TimeOnly(2, 0), kyiv),
                LocalTime(tomorrowInKyiv, new TimeOnly(3, 0), kyiv),
                BookingStatus.Confirmed, "Europe/Kyiv")
        ];

        var roundClockDesk = roundClock.Desks.First();
        roundClockDesk.Bookings =
        [
            BuildBooking("Sofia Marchenko", "sofia@example.com",
                LocalTime(tomorrowInKyiv, new TimeOnly(3, 0), kyiv),
                LocalTime(tomorrowInKyiv, new TimeOnly(4, 0), kyiv),
                BookingStatus.Confirmed, "Europe/Kyiv")
        ];

        return [central, riverside, nightOwl, nightShift, roundClock, havana];
    }

    /// <summary>
    /// Anchors a booking to the coworking's local clock instead of to UtcNow.
    /// </summary>
    private static DateTimeOffset LocalTime(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(time);

        return new DateTimeOffset(local, timeZone.GetUtcOffset(local));
    }

    private static Booking BuildBooking(string userName,
        string userEmail,
        DateTimeOffset start,
        DateTimeOffset end,
        BookingStatus status,
        string userTimeZoneId) =>
        new()
        {
            AccessCode = Guid.NewGuid(),
            UserName = userName,
            UserEmail = userEmail,
            StartTime = start.ToUniversalTime(),
            EndTime = end.ToUniversalTime(),
            Status = status,
            UserTimeZoneId = userTimeZoneId,
            Desk = null!
        };
}
