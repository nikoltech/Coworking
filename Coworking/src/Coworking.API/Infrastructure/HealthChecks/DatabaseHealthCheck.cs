using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Coworking.API.Infrastructure.HealthChecks;

internal sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The database refused the connection.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("The database is unreachable.", ex);
        }
    }
}
