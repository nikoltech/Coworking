using Coworking.Domain.Constants;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Coworking.IntegrationTests;

/// <summary>
/// The duration cap holds in the database itself, whatever the validators say.
/// </summary>
public class BookingDurationLimitTests
{
    private const string Database = "coworking_tests_duration";

    [Fact]
    public async Task BookingAtTheLimit_IsStored()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        await SaveAsync(factory, BookingLimits.MaxDuration);
    }

    [Fact]
    public async Task BookingOverTheLimit_IsRejectedByTheDatabase()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAsync(factory, BookingLimits.MaxDuration + TimeSpan.FromMinutes(1)));

        var postgres = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
    }

    private static async Task SaveAsync(TestApiFactory factory, TimeSpan duration)
    {
        var deskId = await TestSeed.DeskAsync(factory, "Duration");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<Booking>().Add(new Booking
        {
            DeskId = deskId,
            UserName = "Duration Probe",
            UserEmail = "duration@example.com",
            StartTime = TestSeed.DefaultStart,
            EndTime = TestSeed.DefaultStart + duration,
            Status = BookingStatus.PendingPayment
        });

        await db.SaveChangesAsync();
    }
}
