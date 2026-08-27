using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Domain.ValueObjects;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.IntegrationTests;

/// <summary>
/// Seeds straight into the database. There is no cleanup between runs, so every coworking
/// gets a unique name rather than relying on an empty table.
/// </summary>
internal static class TestSeed
{
    public static async Task<int> DeskAsync(TestApiFactory factory, string label)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var coworking = NewCoworking(label);

        db.Set<CoworkingEntity>().Add(coworking);
        await db.SaveChangesAsync();

        return coworking.Desks.First().Id;
    }

    public static async Task<Guid> BookingAsync(TestApiFactory factory, string label, BookingStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        var coworking = NewCoworking(label);

        db.Set<CoworkingEntity>().Add(coworking);
        db.Set<Booking>().Add(NewBooking(coworking.Desks.First(), DefaultStart, status));
        await db.SaveChangesAsync();

        return coworking.Desks.First().Bookings.First().AccessCode;
    }

    /// Seeds a booking onto an existing desk, for tests that then call the API for that slot.
    public static async Task BookingOnDeskAsync(
        TestApiFactory factory,
        int deskId,
        DateTimeOffset start,
        BookingStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var desk = await db.Set<Desk>().SingleAsync(d => d.Id == deskId);

        db.Set<Booking>().Add(NewBooking(desk, start, status));
        await db.SaveChangesAsync();
    }

    public static readonly DateTimeOffset DefaultStart = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(10);

    private static Booking NewBooking(Desk desk, DateTimeOffset start, BookingStatus status) =>
        new()
        {
            Desk = desk,
            UserName = "Seed Probe",
            UserEmail = "seed@example.com",
            StartTime = start,
            EndTime = start.AddHours(1),
            Status = status
        };

    // 24/7 in UTC keeps working-hours and rounding out of the way
    private static CoworkingEntity NewCoworking(string label) =>
        new()
        {
            Name = $"{label} {Guid.NewGuid():N}",
            Address = "Test",
            TimeZoneId = "UTC",
            SlotSize = SlotSize.ThirtyMinutes,
            OpenTime = new TimeOnly(0, 0),
            CloseTime = new TimeOnly(0, 0),
            Desks = [new Desk { Name = "T1", Description = "Test", Coworking = null! }]
        };
}
