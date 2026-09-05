using Coworking.API.Controllers;
using Coworking.Domain.Enums;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Coworking.IntegrationTests;

/// <summary>
/// The retry path end to end: a real 40001 raised at COMMIT, classified by the real
/// PostgresConflictDetector, replayed against a rolled-back transaction. Unit tests cover the
/// policy with a stubbed detector; this covers the wiring around it.
/// </summary>
public class RetryPathTests
{
    private const string Database = "coworking_tests_retry";

    [Fact]
    public async Task SerializationFailureAtCommit_IsRetriedAndTheBookingIsCreated()
    {
        await using var seed = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(seed, "Retry path");

        var interceptor = new CommitFailsInterceptor();

        await using var factory = new TestApiFactory(bypassCoordinator: false, Database,
            services => services.AddSingleton<IInterceptor>(interceptor));

        var start = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(10);

        var response = await Client(factory, "203.0.113.30").PostAsJsonAsync("/api/bookings", new
        {
            deskId,
            userEmail = "retry@example.com",
            userName = "Retry Probe",
            startTime = start,
            endTime = start.AddHours(1),
            metadata = (object?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, interceptor.Failures);
    }

    /// <summary>
    /// A concurrency conflict on the booking row is the only source of 409 on cancellation, and
    /// racing two real requests reaches it only sometimes.
    /// </summary>
    [Fact]
    public async Task ConcurrencyConflictOnCancel_Returns409()
    {
        await using var seed = new TestApiFactory(bypassCoordinator: false, Database);
        var booking = await TestSeed.BookingAsync(seed, "Cancel conflict", BookingStatus.PendingPayment);

        var interceptor = new BookingUpdateConflictsOnceInterceptor();

        await using var factory = new TestApiFactory(bypassCoordinator: false, Database,
            services => services.AddSingleton<IInterceptor>(interceptor));

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/bookings/{booking.Id}");
        request.Headers.Add(BookingsController.AccessCodeHeader, booking.AccessCode.ToString());

        var response = await Client(factory, "203.0.113.31").SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, interceptor.Failures);
    }

    /// <summary>
    /// Contention the retries cannot outlast is still not the caller's fault, so it answers 503
    /// with a spread Retry-After rather than a bare 500.
    /// </summary>
    [Fact]
    public async Task RetriesExhausted_Returns503WithRetryAfter()
    {
        await using var seed = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(seed, "Retry exhausted");

        var interceptor = new CommitFailsInterceptor(times: int.MaxValue);

        await using var factory = new TestApiFactory(bypassCoordinator: false, Database,
            services => services.AddSingleton<IInterceptor>(interceptor));

        var start = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(14);

        var response = await Client(factory, "203.0.113.32").PostAsJsonAsync("/api/bookings", new
        {
            deskId,
            userEmail = "exhausted@example.com",
            userName = "Exhausted Probe",
            startTime = start,
            endTime = start.AddHours(1),
            metadata = (object?)null
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 3);
    }

    private static HttpClient Client(TestApiFactory factory, string clientIp)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", clientIp);

        return client;
    }
}
