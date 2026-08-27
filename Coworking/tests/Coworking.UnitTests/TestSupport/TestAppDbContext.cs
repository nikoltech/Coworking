using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Transactions;
using Coworking.Application.Common.Enums;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests.TestSupport;

/// <summary>
/// A minimal IAppDbContext built from EF conventions. AppDbContext binds its concurrency token
/// to the PostgreSQL xmin column and so cannot run on SQLite; the integration tests cover that
/// mapping, and the handler only depends on the abstraction.
/// </summary>
internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options)
    : DbContext(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>().Property(b => b.Status).HasConversion<string>();

        // an owned value object the cancel path never reads
        modelBuilder.Entity<CoworkingEntity>().Ignore(c => c.SlotSize);
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new EfTransactionWrapper(await Database.BeginTransactionAsync(ct));

    // SQLite has a single isolation level
    public Task<ITransaction> BeginTransactionAsync(TransactionIsolationLevel isolationLevel, CancellationToken ct = default) =>
        BeginTransactionAsync(ct);

    public void DiscardPendingChanges() => ChangeTracker.Clear();
}
