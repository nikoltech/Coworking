using Coworking.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

namespace Coworking.IntegrationTests;

public class SlotBlockingTests
{
    private const string Database = "coworking_tests_blocking";

    /// <summary>
    /// Which bookings still hold a slot, asked through the API rather than through the
    /// specification: only a booking that is neither cancelled nor expired keeps the desk taken.
    /// </summary>
    [Theory]
    [InlineData(BookingStatus.Created, HttpStatusCode.Conflict)]
    [InlineData(BookingStatus.PendingPayment, HttpStatusCode.Conflict)]
    [InlineData(BookingStatus.Confirmed, HttpStatusCode.Conflict)]
    [InlineData(BookingStatus.Cancelled, HttpStatusCode.OK)]
    [InlineData(BookingStatus.Expired, HttpStatusCode.OK)]
    public async Task ExistingBooking_HoldsTheSlotOnlyWhileActive(BookingStatus existing, HttpStatusCode expected)
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var deskId = await TestSeed.DeskAsync(factory, $"Blocking {existing}");
        var start = TestSeed.DefaultStart;

        await TestSeed.BookingOnDeskAsync(factory, deskId, start, existing);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", "203.0.113.40");

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            deskId,
            userEmail = "blocking@example.com",
            userName = "Blocking Probe",
            startTime = start,
            endTime = start.AddHours(1),
            metadata = (object?)null
        });

        Assert.Equal(expected, response.StatusCode);
    }
}
