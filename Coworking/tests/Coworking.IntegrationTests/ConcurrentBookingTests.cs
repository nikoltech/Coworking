using Coworking.Domain.Entities;
using Coworking.Domain.ValueObjects;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.IntegrationTests;

public class ConcurrentBookingTests
{
    /// <summary>
    /// Fast path: InMemoryBookingAccessCoordinator serializes the two requests before the
    /// database, so the loser sees the committed booking and is rejected by the overlap check.
    /// </summary>
    [Fact]
    public Task OverlappingRequests_WithCoordinator_LeaveExactlyOneWinner() =>
        AssertExactlyOneConflict(bypassCoordinator: false, clientIp: "203.0.113.1");

    /// <summary>
    /// Database path: without the lease both transactions run under Serializable at once, so
    /// one hits 40001. Proves the retry behavior catches it and it never surfaces as a 500.
    /// </summary>
    [Fact]
    public Task OverlappingRequests_WithoutCoordinator_LeaveExactlyOneWinner() =>
        AssertExactlyOneConflict(bypassCoordinator: true, clientIp: "203.0.113.2");

    private static async Task AssertExactlyOneConflict(bool bypassCoordinator, string clientIp)
    {
        await using var factory = new TestApiFactory(bypassCoordinator);

        var deskId = await SeedDeskAsync(factory);
        var start = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(10);

        var first = PostBookingAsync(factory, clientIp, deskId, start, start.AddHours(1));
        var second = PostBookingAsync(factory, clientIp, deskId, start.AddMinutes(30), start.AddMinutes(90));

        var results = await Task.WhenAll(first, second);

        var report = string.Join("\n", results.Select(r => $"  {(int)r.Status} {r.Status}: {r.Body}"));

        Assert.False(
            results.Any(r => (int)r.Status >= 500),
            $"A conflict leaked as a server error:\n{report}");

        Assert.Equal(1, results.Count(r => r.Status == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.Status == HttpStatusCode.Conflict));
    }

    /****************************************************************
     * helpers
     *******************************************************/

    private static async Task<(HttpStatusCode Status, string Body)> PostBookingAsync(
        TestApiFactory factory,
        string clientIp,
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        // distinct IP per test: the booking-write limiter partitions on it (10/min)
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", clientIp);

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            deskId,
            userEmail = "concurrency@example.com",
            userName = "Concurrency Probe",
            startTime = start,
            endTime = end,
            metadata = (object?)null
        });

        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<int> SeedDeskAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        // 24/7 in UTC keeps working-hours and rounding out of the way
        var coworking = new CoworkingEntity
        {
            Name = $"Concurrency {Guid.NewGuid():N}",
            Address = "Test",
            TimeZoneId = "UTC",
            SlotSize = SlotSize.ThirtyMinutes,
            OpenTime = new TimeOnly(0, 0),
            CloseTime = new TimeOnly(0, 0),
            Desks = [new Desk { Name = "T1", Description = "Test", Coworking = null! }]
        };

        db.Set<CoworkingEntity>().Add(coworking);
        await db.SaveChangesAsync();

        return coworking.Desks.First().Id;
    }
}
