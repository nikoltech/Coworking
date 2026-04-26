using Coworking.API.Infrastructure.Extensions;
using Coworking.API.Infrastructure.Extensions.Security;
using Coworking.API.Infrastructure.RateLimiting;

namespace Coworking.API;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddProxySettings(configuration);
        services.AddApiRateLimiting();

        services.ConfigureErrorHandling();
        services.AddControllers();

        services.AddHttpContextAccessor();

        services.AddCors(configuration);

        services.AddApiServices();



        return services;
    }
}
