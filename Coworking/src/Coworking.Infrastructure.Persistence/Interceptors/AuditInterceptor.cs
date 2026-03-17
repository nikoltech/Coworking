using Coworking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Coworking.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, ct);

        // TODO: what wel`ll do in distributed environment with different timezones? 
        /* Note: time consistensy, different timezones can give issues in distributed environment.
         * Maybe we should use DateTimeOffset instead of DateTime?
         * Or/and find another solution to keep time consistent across different environments.
         */
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<ITrackEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
