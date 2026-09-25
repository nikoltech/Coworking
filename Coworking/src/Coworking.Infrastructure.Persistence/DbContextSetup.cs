using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence;

internal static class DbContextSetup
{
    /// <summary>
    /// Shared by the runtime registration and the design-time factory: any difference here makes
    /// the design-time model diverge from the runtime one and trips PendingModelChangesWarning.
    /// </summary>
    public static DbContextOptionsBuilder UseAppDatabase(this DbContextOptionsBuilder builder,
        string? connectionString) =>
        builder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();
}
