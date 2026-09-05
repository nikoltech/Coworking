using Coworking.API.Infrastructure.Extensions;
using Coworking.API.Infrastructure.Extensions.Security;

namespace Coworking.API;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureApi(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddApiServices();
        services.ConfigureErrorHandling();

        services.AddHttpContextAccessor();
        services.AddAppLocalization(configuration);

        services.AddProxySettings(configuration);
        services.AddCors(configuration);
        services.AddApiRateLimiting();

        services.AddAppHealthChecks();

        // MassTransit needs it to carry trace context into the outbox
        services.AddTraceContext();

        return services;
    }
}
