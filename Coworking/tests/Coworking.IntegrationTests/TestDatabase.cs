using System.Collections.Concurrent;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.IntegrationTests;

/// <summary>
/// TestDatabase rebuilds each test database from the current model the first time a run
/// touches it. EnsureCreated alone skips a database that already exists, so a schema left by
/// an older model would survive.
/// </summary>
internal static class TestDatabase
{
    private static readonly ConcurrentDictionary<string, Lazy<Task>> Recreated = new();

    public static Task EnsureFreshAsync(AppDbContext db)
    {
        var name = db.Database.GetDbConnection().Database;

        // Lazy, because GetOrAdd may run its factory twice under contention
        return Recreated.GetOrAdd(name, _ => new Lazy<Task>(() => RecreateAsync(db))).Value;
    }

    private static async Task RecreateAsync(AppDbContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
