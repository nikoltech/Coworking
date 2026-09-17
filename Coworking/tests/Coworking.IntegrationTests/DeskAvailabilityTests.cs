using Coworking.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

namespace Coworking.IntegrationTests;

/// <summary>
/// The availability endpoint end to end: the booking query runs on real PostgreSQL, and the
/// response keeps each border in the coworking's local offset.
/// </summary>
public class DeskAvailabilityTests
{
    private const string Database = "coworking_tests_availability";

    // a summer date, so Kyiv is at +03:00
    private static readonly DateOnly Day = new(2030, 7, 1);

    private sealed record Interval(DateTimeOffset Start, DateTimeOffset End, bool IsAvailable);

    private sealed record Availability(
        int DeskId, int SlotSizeMinutes, List<Interval> Intervals, int TotalSlots, int AvailableSlots);

    [Fact]
    public async Task DayHours_WithoutBookings_ReturnsOneFreeWindowPerDay()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Availability day", Hours(9, 18));

        var availability = await GetAsync(factory, deskId, Day, Day.AddDays(1));

        Assert.Equal(30, availability.SlotSizeMinutes);
        Assert.Equal(
            [
                new Interval(Utc(Day, 9), Utc(Day, 18), true),
                new Interval(Utc(Day.AddDays(1), 9), Utc(Day.AddDays(1), 18), true)
            ],
            availability.Intervals);
        Assert.Equal((36, 36), (availability.TotalSlots, availability.AvailableSlots));
    }

    [Fact]
    public async Task NonStop_ReturnsOneWindowFromMidnightToMidnight()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Availability non-stop");

        var availability = await GetAsync(factory, deskId, Day, Day);

        Assert.Equal([new Interval(Utc(Day, 0), Utc(Day.AddDays(1), 0), true)], availability.Intervals);
    }

    [Theory]
    [InlineData(BookingStatus.PendingPayment, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.Expired, false)]
    public async Task Booking_IsBusyOnlyWhileActive(BookingStatus status, bool busy)
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, $"Availability {status}", Hours(9, 18));
        await TestSeed.BookingOnDeskAsync(factory, deskId, Utc(Day, 10), status);

        var availability = await GetAsync(factory, deskId, Day, Day);

        var expected = busy
            ? new List<Interval>
            {
                new(Utc(Day, 9), Utc(Day, 10), true),
                new(Utc(Day, 10), Utc(Day, 11), false),
                new(Utc(Day, 11), Utc(Day, 18), true)
            }
            : [new(Utc(Day, 9), Utc(Day, 18), true)];

        Assert.Equal(expected, availability.Intervals);
    }

    [Fact]
    public async Task NightHours_BookingAfterMidnightOfTheLastDay_IsBusy()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Availability night", Hours(22, 6));
        await TestSeed.BookingOnDeskAsync(factory, deskId, Utc(Day.AddDays(1), 2), BookingStatus.Confirmed);

        var availability = await GetAsync(factory, deskId, Day, Day);

        Assert.Equal(
            [
                new Interval(Utc(Day, 22), Utc(Day.AddDays(1), 2), true),
                new Interval(Utc(Day.AddDays(1), 2), Utc(Day.AddDays(1), 3), false),
                new Interval(Utc(Day.AddDays(1), 3), Utc(Day.AddDays(1), 6), true)
            ],
            availability.Intervals);
    }

    // records compare instants only, so the offsets are checked separately
    [Fact]
    public async Task LocalZone_DaysAndLabelsFollowTheCoworkingOffset()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Availability Kyiv", Hours(9, 18, "Europe/Kyiv"));

        // 10:00 in Kyiv
        await TestSeed.BookingOnDeskAsync(factory, deskId, Utc(Day, 7), BookingStatus.Confirmed);

        var availability = await GetAsync(factory, deskId, Day, Day);

        var kyiv = TimeSpan.FromHours(3);
        Assert.Equal(
            [
                (Local(9, kyiv), true),
                (Local(10, kyiv), false),
                (Local(11, kyiv), true)
            ],
            availability.Intervals.Select(i => ((i.Start.DateTime, i.Start.Offset), i.IsAvailable)));
        Assert.Equal(Local(18, kyiv), (availability.Intervals[^1].End.DateTime, availability.Intervals[^1].End.Offset));
    }

    [Fact]
    public async Task BookingOfAnotherDesk_IsNotShown()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var deskId = await TestSeed.DeskAsync(factory, "Availability own", Hours(9, 18));
        var otherDeskId = await TestSeed.DeskAsync(factory, "Availability other", Hours(9, 18));
        await TestSeed.BookingOnDeskAsync(factory, otherDeskId, Utc(Day, 10), BookingStatus.Confirmed);

        var availability = await GetAsync(factory, deskId, Day, Day);

        Assert.All(availability.Intervals, i => Assert.True(i.IsAvailable));
    }

    [Fact]
    public async Task UnknownDesk_Returns404()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        await TestSeed.DeskAsync(factory, "Availability unknown");

        var response = await Client(factory).GetAsync($"/api/desks/999999999/availability?dateFrom={Day:O}&dateTo={Day:O}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("1", "2030-07-02", "2030-07-01")]
    [InlineData("1", "2030-07-01", "2030-09-29")]
    [InlineData("1", "2030-07-01", null)]
    [InlineData("0", "2030-07-01", "2030-07-01")]
    public async Task InvalidRequest_Returns400(string deskId, string? dateFrom, string? dateTo)
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var query = string.Join('&',
            new[] { ("dateFrom", dateFrom), ("dateTo", dateTo) }
                .Where(p => p.Item2 is not null)
                .Select(p => $"{p.Item1}={p.Item2}"));

        var response = await Client(factory).GetAsync($"/api/desks/{deskId}/availability?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<Availability> GetAsync(TestApiFactory factory, int deskId, DateOnly from, DateOnly to)
    {
        var response = await Client(factory).GetAsync($"/api/desks/{deskId}/availability?dateFrom={from:O}&dateTo={to:O}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<Availability>())!;
    }

    private static HttpClient Client(TestApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", "203.0.113.50");

        return client;
    }

    private static Action<Domain.Entities.Coworking> Hours(int open, int close, string timeZoneId = "UTC") =>
        coworking =>
        {
            coworking.IsNonStop = false;
            coworking.OpenTime = new TimeOnly(open, 0);
            coworking.CloseTime = new TimeOnly(close, 0);
            coworking.TimeZoneId = timeZoneId;
        };

    private static DateTimeOffset Utc(DateOnly day, int hour) =>
        new(day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private static (DateTime, TimeSpan) Local(int hour, TimeSpan offset) =>
        (Day.ToDateTime(new TimeOnly(hour, 0)), offset);
}
