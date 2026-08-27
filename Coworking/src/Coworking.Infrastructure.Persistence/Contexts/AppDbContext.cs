using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Transactions;
using Coworking.Application.Common.Enums;
using Coworking.Infrastructure.Persistence.Configurations.Common;
using Coworking.Infrastructure.Persistence.Extensions;
using Coworking.Infrastructure.Persistence.Transactions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.ApplyGlobalConfiguration();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetUtcConverter>();

        configurationBuilder
            .Properties<DateTimeOffset?>()
            .HaveConversion<DateTimeOffsetUtcConverter>();
    }


    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var efTransaction = await Database.BeginTransactionAsync(ct);
        return new EfTransactionWrapper(efTransaction);
    }

    public async Task<ITransaction> BeginTransactionAsync(
        TransactionIsolationLevel isolationLevel,
        CancellationToken ct = default)
    {
        var efTransaction = await Database.BeginTransactionAsync(isolationLevel.ToSqlType(), ct);
        return new EfTransactionWrapper(efTransaction);
    }

    /// <summary>
    /// OutboxState is carried over: MassTransit creates one per scope and never rebuilds it,
    /// so clearing it leaves the replayed publish writing OutboxMessage rows with no state
    /// row to point at.
    /// </summary>
    public void DiscardPendingChanges()
    {
        var outboxStates = ChangeTracker.Entries<OutboxState>()
            .Select(entry => entry.Entity)
            .ToArray();

        ChangeTracker.Clear();

        foreach (var outboxState in outboxStates)
            Add(outboxState);
    }
}
