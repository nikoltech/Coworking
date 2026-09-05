using Coworking.API.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

namespace Coworking.API.Infrastructure.Extensions;

public static class HealthCheckExtensions
{
    private const string Ready = "ready";

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [Ready]);

        return services;
    }

    /// <summary>
    /// Liveness answers whether the process itself is up, so it runs no checks at all: probing
    /// dependencies here would have the orchestrator restart a healthy app every time the
    /// database blinks. Readiness is where dependencies belong — it only removes the instance
    /// from rotation.
    /// </summary>
    public static WebApplication MapAppHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(Ready)
        }).DisableRateLimiting();

        return app;
    }
}
