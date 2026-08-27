using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Messaging.Contracts;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.IntegrationTests;

/// <summary>
/// The outbox stages its rows in the same change tracker a retry has to empty, but creates its
/// OutboxState only once per scope. Losing that row leaves the replayed publish writing an
/// OutboxMessage that points nowhere.
/// </summary>
public class OutboxRetryTests
{
    private const string Database = "coworking_tests_outbox";

    private static readonly BookingCreatedMessage Message = new(
        "outbox@example.com", "Outbox Probe", "T1", "Test", DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddHours(1), "UTC");

    /// The attempt failed before saving, so OutboxState is still Added.
    [Fact]
    public async Task DiscardAfterStaging_KeepsOutboxStateForReplay()
    {
        var (scope, db, publisher) = await ArrangeAsync();
        using var _ = scope;

        await publisher.Publish(Message);
        Assert.Equal(1, StagedCount<OutboxState>(db));
        Assert.Equal(1, StagedCount<OutboxMessage>(db));

        db.DiscardPendingChanges();

        Assert.Equal(1, StagedCount<OutboxState>(db));
        Assert.Equal(0, StagedCount<OutboxMessage>(db));

        await AssertReplaySavesAsync(db, publisher);
    }

    /// <summary>
    /// What actually happens in production: 40001 surfaces at COMMIT, so SaveChanges already
    /// ran and OutboxState is Unchanged rather than Added when the tracker is reset.
    /// </summary>
    [Fact]
    public async Task DiscardAfterSave_KeepsOutboxStateForReplay()
    {
        var (scope, db, publisher) = await ArrangeAsync();
        using var _ = scope;

        await using (var rolledBack = await db.Database.BeginTransactionAsync())
        {
            await publisher.Publish(Message);
            await db.SaveChangesAsync();
            await rolledBack.RollbackAsync();
        }

        Assert.Equal(0, StagedCount<OutboxState>(db));
        Assert.Equal(EntityState.Unchanged, db.ChangeTracker.Entries<OutboxState>().Single().State);

        db.DiscardPendingChanges();

        Assert.Equal(1, StagedCount<OutboxState>(db));

        await AssertReplaySavesAsync(db, publisher);
    }

    // helpers

    private static async Task<(IServiceScope Scope, AppDbContext Db, IPublishEndpoint Publisher)> ArrangeAsync()
    {
        var factory = new TestApiFactory(bypassCoordinator: false, Database);
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        return (scope, db, scope.ServiceProvider.GetRequiredService<IPublishEndpoint>());
    }

    /// The replayed publish must find its state row, or SaveChanges breaks the outbox foreign key.
    private static async Task AssertReplaySavesAsync(AppDbContext db, IPublishEndpoint publisher)
    {
        await publisher.Publish(Message);

        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.SaveChangesAsync();
        await transaction.RollbackAsync();
    }

    private static int StagedCount<TEntity>(AppDbContext db) where TEntity : class =>
        db.ChangeTracker.Entries<TEntity>().Count(e => e.State == EntityState.Added);
}
