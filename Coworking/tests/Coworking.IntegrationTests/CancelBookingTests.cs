using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Infrastructure.Persistence.Contexts;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Coworking.IntegrationTests;

/// <summary>
/// DELETE /api/bookings/{accessCode}. The access code is the authorization, so an unknown
/// code and someone else's code are the same answer: 404.
/// </summary>
public class CancelBookingTests
{
    private const string Database = "coworking_tests_cancel";

    [Theory]
    [InlineData(BookingStatus.Created, HttpStatusCode.NoContent)]
    [InlineData(BookingStatus.PendingPayment, HttpStatusCode.NoContent)]
    [InlineData(BookingStatus.PendingConfirmation, HttpStatusCode.NoContent)]
    [InlineData(BookingStatus.Confirmed, HttpStatusCode.NoContent)]
    [InlineData(BookingStatus.Cancelled, HttpStatusCode.UnprocessableEntity)]
    [InlineData(BookingStatus.Expired, HttpStatusCode.UnprocessableEntity)]
    public async Task Delete_ByBookingState_ReturnsExpectedStatus(BookingStatus seeded, HttpStatusCode expected)
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var accessCode = await TestSeed.BookingAsync(factory, "Cancel state", seeded);

        var response = await Client(factory, "203.0.113.10").DeleteAsync(Url(accessCode));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ActiveBooking_PersistsCancelledAndWritesOutboxMessage()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var accessCode = await TestSeed.BookingAsync(factory, "Cancel outbox", BookingStatus.PendingPayment);

        var before = await OutboxCountAsync(factory);

        var response = await Client(factory, "203.0.113.11").DeleteAsync(Url(accessCode));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
        Assert.Equal(BookingStatus.Cancelled, await StatusAsync(factory, accessCode));

        // the row is only there if Publish ran inside the same transaction as the status change
        Assert.True(await OutboxCountAsync(factory) > before);
    }

    [Fact]
    public async Task Delete_UnknownAccessCode_Returns404()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var response = await Client(factory, "203.0.113.12").DeleteAsync(Url(Guid.CreateVersion7()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not Found", await TitleAsync(response));
    }

    [Fact]
    public async Task Delete_EmptyGuid_Returns400()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var response = await Client(factory, "203.0.113.13").DeleteAsync(Url(Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Twice_SecondIsRejected()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var accessCode = await TestSeed.BookingAsync(factory, "Cancel twice", BookingStatus.PendingPayment);
        var client = Client(factory, "203.0.113.14");

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(Url(accessCode))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.DeleteAsync(Url(accessCode))).StatusCode);
    }

    /// <summary>
    /// The point of cancelling, through the endpoint that does it: the desk is free again.
    /// </summary>
    [Fact]
    public async Task Delete_CancelledBooking_FreesTheSlot()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var deskId = await TestSeed.DeskAsync(factory, "Cancel frees");
        var client = Client(factory, "203.0.113.15");
        var start = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(10);

        var created = await CreateBookingAsync(client, deskId, start);
        Assert.Equal(HttpStatusCode.OK, created.Status);

        var taken = await CreateBookingAsync(client, deskId, start);
        Assert.Equal(HttpStatusCode.Conflict, taken.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(Url(created.AccessCode))).StatusCode);

        var reBooked = await CreateBookingAsync(client, deskId, start);
        Assert.Equal(HttpStatusCode.OK, reBooked.Status);
    }

    /// <summary>
    /// Both requests read the same row, so without the xmin token the second would silently
    /// overwrite the first. Which rejection the loser gets depends on whether it read the row
    /// before or after the winner committed: 409 from the token, 422 from the state graph.
    /// </summary>
    [Fact]
    public async Task Delete_TwoConcurrentRequests_LeaveExactlyOneWinner()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var accessCode = await TestSeed.BookingAsync(factory, "Cancel race", BookingStatus.PendingPayment);
        var client = Client(factory, "203.0.113.16");

        var results = await Task.WhenAll(
            client.DeleteAsync(Url(accessCode)),
            client.DeleteAsync(Url(accessCode)));

        var report = string.Join(", ", results.Select(r => (int)r.StatusCode));

        Assert.False(results.Any(r => (int)r.StatusCode >= 500), $"A conflict leaked as a server error: {report}");
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, results.Count(r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity));
        Assert.Equal(BookingStatus.Cancelled, await StatusAsync(factory, accessCode));
    }

    // helpers

    private static string Url(Guid accessCode) => $"/api/bookings/{accessCode}";

    // distinct IP per test: the booking-write limiter partitions on it (10/min)
    private static HttpClient Client(TestApiFactory factory, string clientIp)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", clientIp);

        return client;
    }

    private static async Task<(HttpStatusCode Status, Guid AccessCode)> CreateBookingAsync(
        HttpClient client,
        int deskId,
        DateTimeOffset start)
    {
        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            deskId,
            userEmail = "cancel@example.com",
            userName = "Cancel Probe",
            startTime = start,
            endTime = start.AddHours(1),
            metadata = (object?)null
        });

        if (response.StatusCode != HttpStatusCode.OK)
            return (response.StatusCode, Guid.Empty);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return (response.StatusCode, body.RootElement.GetProperty("accessCode").GetGuid());
    }

    private static async Task<string?> TitleAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return body.RootElement.GetProperty("title").GetString();
    }

    private static async Task<BookingStatus> StatusAsync(TestApiFactory factory, Guid accessCode)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Set<Booking>()
            .Where(b => b.AccessCode == accessCode)
            .Select(b => b.Status)
            .SingleAsync();
    }

    private static async Task<int> OutboxCountAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Set<OutboxMessage>().CountAsync();
    }
}
