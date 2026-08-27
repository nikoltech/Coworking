using System.Net;
using System.Net.Http.Json;

namespace Coworking.IntegrationTests;

public class ConcurrentBookingTests
{
    private const string Database = "coworking_tests_concurrent";

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

    /// <summary>
    /// Non-overlapping slots on one desk: both bookings are legal, but their overlap-check
    /// predicates cover the same index range, so Serializable aborts one with 40001. The
    /// retried attempt reaches the outbox, which is the path OutboxRetryTests pins down.
    /// </summary>
    [Fact]
    public async Task NonOverlappingRequests_ThatSerializationConflict_BothSucceed()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: true, Database);

        var deskId = await TestSeed.DeskAsync(factory, "Retry outbox");
        var start = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(10);

        var results = await Task.WhenAll(
            PostBookingAsync(factory, "203.0.113.3", deskId, start, start.AddHours(1)),
            PostBookingAsync(factory, "203.0.113.4", deskId, start.AddHours(2), start.AddHours(3)));

        var report = string.Join("\n", results.Select(r => $"  {(int)r.Status} {r.Status}: {r.Body}"));

        Assert.True(results.All(r => r.Status == HttpStatusCode.OK),
            $"Both bookings are legal, so both must be created:\n{report}");
    }

    private static async Task AssertExactlyOneConflict(bool bypassCoordinator, string clientIp)
    {
        await using var factory = new TestApiFactory(bypassCoordinator, Database);

        var deskId = await TestSeed.DeskAsync(factory, "Concurrency");
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

    // helpers

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
}
