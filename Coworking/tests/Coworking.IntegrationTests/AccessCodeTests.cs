using Coworking.Domain.Enums;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net.Http.Json;

namespace Coworking.IntegrationTests;

/// <summary>
/// The access code is generated on save, so version 7 is proven where it is produced: through the
/// API, through the context, and against the database, which refuses any other version.
/// </summary>
public class AccessCodeTests
{
    private const string Database = "coworking_tests_access_code";

    private sealed record CreatedBooking(Guid AccessCode, long BookingId);

    [Fact]
    public async Task BookingCreatedThroughTheApi_ReturnsVersion7()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Access code api");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", "203.0.113.60");

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            deskId,
            userEmail = "access@example.com",
            userName = "Access Probe",
            startTime = TestSeed.DefaultStart,
            endTime = TestSeed.DefaultStart.AddHours(1),
            metadata = (object?)null
        });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreatedBooking>();

        Assert.Equal(7, created!.AccessCode.Version);
    }

    [Fact]
    public async Task BookingSavedThroughTheContext_GetsVersion7()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var (_, accessCode) = await TestSeed.BookingAsync(factory, "Access code", BookingStatus.Confirmed);

        Assert.Equal(7, accessCode.Version);
    }

    [Fact]
    public async Task AccessCodeOfAnotherVersion_IsRejectedByTheDatabase()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Access code raw");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // gen_random_uuid() is version 4, and raw SQL is the only way left to smuggle one in
        var ex = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO bookings (desk_id, user_name, user_email, start_time, end_time, status, access_code, created_at)
            VALUES ({0}, 'Probe', 'probe@example.com', {1}, {2}, 'PendingPayment', gen_random_uuid(), now())
            """,
            // raw SQL bypasses the converter, so the parameters must already be UTC
            deskId, TestSeed.DefaultStart.ToUniversalTime(), TestSeed.DefaultStart.AddHours(1).ToUniversalTime()));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }
}
