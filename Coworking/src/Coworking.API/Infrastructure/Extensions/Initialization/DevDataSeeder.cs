using Coworking.Domain.Common;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Domain.ValueObjects;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.API.Infrastructure.Extensions.Initialization;

/// <summary>
/// Throwaway helper for filling/wiping the DB during local experiments.
/// To remove the feature: delete this file, the dev seed block in
/// AppInitializationExtensions, and the General:SeedData/ResetData flags.
/// </summary>
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

        await transaction.CommitAsync(CancellationToken.None);
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

    private static List<Coworking.Domain.Entities.Coworking> BuildSeedGraph()
    {
        // BookingTimeInterceptor rounds this to whole minutes on save
        var now = DateTimeOffset.UtcNow;

        var central = new Coworking.Domain.Entities.Coworking
        {
            Name = "Central Hub",
            Address = "12 Khreshchatyk St, Kyiv",
            Description = "Regular day hours with a whole-hour offset.",
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
            Description = "Regular day hours with a one-hour slot.",
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

        var nightOwl = new Coworking.Domain.Entities.Coworking
        {
            Name = "Night Owl 24/7",
            Address = "8 Lukyanivska St, Kyiv",
            Description = "Non-stop; the booking on the nearest DST day lasts 1 or 3 real hours.",
            TimeZoneId = "Europe/Kyiv",
            SlotSize = SlotSize.SixtyMinutes,
            IsNonStop = true,
            Desks =
            [
                new Desk { Name = "C1", Description = "Pod #1", Coworking = null! },
                new Desk { Name = "C2", Description = "Pod #2", Coworking = null! }
            ]
        };

        var nightShift = new Coworking.Domain.Entities.Coworking
        {
            Name = "Night Shift",
            Address = "3 Politekhnichna St, Kyiv",
            Description = "Night window across midnight, not a whole number of slots (8h30 / 60 min).",
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

        var roundClock = new Coworking.Domain.Entities.Coworking
        {
            Name = "Round Clock",
            Address = "17 Vasylkivska St, Kyiv",
            Description = "Non-stop; was 08:00–08:00 before the non-stop flag, now starts its day at midnight.",
            TimeZoneId = "Europe/Kyiv",
            SlotSize = SlotSize.SixtyMinutes,
            IsNonStop = true,
            Desks =
            [
                new Desk { Name = "E1", Description = "Always open", Coworking = null! }
            ]
        };

        var havana = new Coworking.Domain.Entities.Coworking
        {
            Name = "Havana Patio",
            Address = "220 Calle Obispo, Havana",
            Description = "Non-stop; DST switches at 00:00, so local midnight is missing on transition days.",
            TimeZoneId = "America/Havana",
            SlotSize = SlotSize.SixtyMinutes,
            IsNonStop = true,
            Desks =
            [
                new Desk { Name = "F1", Description = "Courtyard", Coworking = null! }
            ]
        };

        var kathmandu = new Coworking.Domain.Entities.Coworking
        {
            Name = "Kathmandu Loft",
            Address = "14 Thamel Marg, Kathmandu",
            Description = "Slot grid under a +05:45 offset.",
            TimeZoneId = "Asia/Kathmandu",
            SlotSize = SlotSize.ThirtyMinutes,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(18, 0),
            Desks =
            [
                new Desk { Name = "G1", Description = "Mountain view", Coworking = null! }
            ]
        };

        var mumbai = new Coworking.Domain.Entities.Coworking
        {
            Name = "Mumbai Desk",
            Address = "21 Marine Drive, Mumbai",
            Description = "Slot grid under a +05:30 offset with a one-hour slot.",
            TimeZoneId = "Asia/Kolkata",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(18, 0),
            Desks =
            [
                new Desk { Name = "H1", Description = "Sea breeze", Coworking = null! }
            ]
        };

        var bangalore = new Coworking.Domain.Entities.Coworking
        {
            Name = "Bangalore Flex",
            Address = "7 MG Road, Bengaluru",
            Description = "Grid anchored at 09:30; the last 25-minute slot is clipped at closing.",
            TimeZoneId = "Asia/Kolkata",
            SlotSize = SlotSize.From(25),
            OpenTime = new TimeOnly(9, 30),
            CloseTime = new TimeOnly(18, 0),
            Desks =
            [
                new Desk { Name = "I1", Description = "Hot desk", Coworking = null! }
            ]
        };

        var lordHowe = new Coworking.Domain.Entities.Coworking
        {
            Name = "Lord Howe Hut",
            Address = "1 Lagoon Rd, Lord Howe Island",
            Description = "Non-stop; DST shifts the clock by only 30 minutes.",
            TimeZoneId = "Australia/Lord_Howe",
            SlotSize = SlotSize.ThirtyMinutes,
            IsNonStop = true,
            Desks =
            [
                new Desk { Name = "J1", Description = "Beach hut", Coworking = null! }
            ]
        };

        var stJohns = new Coworking.Domain.Entities.Coworking
        {
            Name = "St. John's Harbour",
            Address = "40 Water St, St. John's",
            Description = "Negative half-hour offset (-03:30) with DST.",
            TimeZoneId = "America/St_Johns",
            SlotSize = SlotSize.SixtyMinutes,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0),
            Desks =
            [
                new Desk { Name = "K1", Description = "Harbour window", Coworking = null! }
            ]
        };

        var chatham = new Coworking.Domain.Entities.Coworking
        {
            Name = "Chatham Point",
            Address = "3 Waitangi Wharf Rd, Chatham Islands",
            Description = "Night hours under a +12:45/+13:45 offset with DST, next to the date line.",
            TimeZoneId = "Pacific/Chatham",
            SlotSize = SlotSize.ThirtyMinutes,
            OpenTime = new TimeOnly(22, 0),
            CloseTime = new TimeOnly(6, 0),
            Desks =
            [
                new Desk { Name = "L1", Description = "Night watch", Coworking = null! }
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

        // 02:00–04:00 local spans the switch: 1 real hour in spring, 3 in autumn
        if (NextTransitionDay(kyiv, tomorrowInKyiv) is { } transitionDay)
        {
            nightOwlDesk2.Bookings.Add(BuildBooking("Oleh Hrytsenko", "oleh@example.com",
                LocalTime(transitionDay, new TimeOnly(2, 0), kyiv),
                LocalTime(transitionDay, new TimeOnly(4, 0), kyiv),
                BookingStatus.Confirmed, "Europe/Kyiv"));
        }

        return [central, riverside, nightOwl, nightShift, roundClock, havana, kathmandu, mumbai, bangalore, lordHowe, stJohns, chatham];
    }

    private static DateOnly? NextTransitionDay(TimeZoneInfo timeZone, DateOnly from)
    {
        for (var day = from; day < from.AddYears(1); day = day.AddDays(1))
        {
            var offsetToday = timeZone.GetUtcOffset(day.ToDateTime(TimeOnly.MinValue));
            var offsetTomorrow = timeZone.GetUtcOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue));

            if (offsetToday != offsetTomorrow)
                return day;
        }

        return null;
    }

    /// <summary>
    /// Anchors a booking to the coworking's local clock instead of to UtcNow.
    /// </summary>
    private static DateTimeOffset LocalTime(DateOnly date, TimeOnly time, TimeZoneInfo timeZone) =>
        ZonedTime.FromWallClock(date.ToDateTime(time), timeZone);

    private static Booking BuildBooking(string userName,
        string userEmail,
        DateTimeOffset start,
        DateTimeOffset end,
        BookingStatus status,
        string userTimeZoneId) =>
        new()
        {
            UserName = userName,
            UserEmail = userEmail,
            StartTime = start.ToUniversalTime(),
            EndTime = end.ToUniversalTime(),
            Status = status,
            UserTimeZoneId = userTimeZoneId,
            Desk = null!
        };
}
