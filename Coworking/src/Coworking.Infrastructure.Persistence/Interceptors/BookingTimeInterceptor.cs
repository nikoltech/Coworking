using Coworking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Coworking.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Last line of defense for whole-minute alignment: 
/// enforce whole-minute booking times at write time
/// </summary>
public sealed class BookingTimeInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context == null)
            return base.SavingChangesAsync(eventData, result, ct);

        foreach (var entry in context.ChangeTracker.Entries<Booking>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            entry.Entity.StartTime = TruncateToMinute(entry.Entity.StartTime);
            entry.Entity.EndTime = TruncateToMinute(entry.Entity.EndTime);
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static DateTimeOffset TruncateToMinute(DateTimeOffset time) =>
        time.AddTicks(-(time.Ticks % TimeSpan.TicksPerMinute));
}
