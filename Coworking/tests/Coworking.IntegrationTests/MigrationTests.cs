using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.IntegrationTests;

public class MigrationTests
{
    private const string Database = "coworking_tests_migrations";

    /// <summary>
    /// Fails when the model was changed without scaffolding a migration. Integration tests
    /// build their schema with EnsureCreated, straight from the model, so nothing else here
    /// would ever notice the gap — but production applies the migration files.
    /// </summary>
    [Fact]
    public void EveryModelChange_HasAGeneratedMigration()
    {
        using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    /// <summary>
    /// Runs the migration files themselves. A migration can match the model perfectly and still
    /// be rejected by PostgreSQL — renaming a column to a system column name, for one.
    /// </summary>
    [Fact]
    public async Task EveryMigration_AppliesToAnEmptyDatabase()
    {
        await using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
