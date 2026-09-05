using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.IntegrationTests;

/// <summary>
/// Booking, Desk and Coworking carry the PostgreSQL xmin system column as a concurrency token,
/// so a writer holding a stale copy of a row must be rejected instead of overwriting it.
/// </summary>
public class ConcurrencyTokenTests
{
    private const string Database = "coworking_tests_concurrency";

    [Fact]
    public async Task ConcurrentUpdate_SecondWriterIsRejected()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);

        var (_, accessCode) = await TestSeed.BookingAsync(factory, "Token", BookingStatus.PendingPayment);

        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();

        var winner = await LoadAsync(first, accessCode);
        var loser = await LoadAsync(second, accessCode);

        winner.Entity.UserName = "Winner";
        await winner.Db.SaveChangesAsync();

        loser.Entity.UserName = "Loser";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => loser.Db.SaveChangesAsync());
    }

    private static async Task<(AppDbContext Db, Booking Entity)> LoadAsync(IServiceScope scope, Guid accessCode)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return (db, await db.Set<Booking>().SingleAsync(b => b.AccessCode == accessCode));
    }
}
