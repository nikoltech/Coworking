using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;

namespace Coworking.IntegrationTests;

/// <summary>
/// Fails COMMIT with a real serialization failure. That is where PostgreSQL raises 40001 in this
/// codebase, so the retry sees the same shape it sees in production. Fail more times than the
/// policy allows to reach the exhausted path.
/// </summary>
internal sealed class CommitFailsInterceptor(int times = 1) : DbTransactionInterceptor
{
    public int Failures { get; private set; }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (Failures >= times)
            return base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);

        Failures++;

        throw new PostgresException(
            "could not serialize access due to read/write dependencies among transactions",
            "ERROR", "ERROR", "40001");
    }
}

/// <summary>
/// Raises a concurrency conflict the first time a booking is updated, so the 409 branch can be
/// reached without racing two real requests. Insert-only work, seeding included, is untouched.
/// </summary>
internal sealed class BookingUpdateConflictsOnceInterceptor : SaveChangesInterceptor
{
    public int Failures { get; private set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var updatesABooking = eventData.Context?.ChangeTracker
            .Entries<Coworking.Domain.Entities.Booking>()
            .Any(entry => entry.State == EntityState.Modified) == true;

        if (Failures > 0 || !updatesABooking)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        Failures++;

        throw new DbUpdateConcurrencyException("the row was modified by another transaction");
    }
}
