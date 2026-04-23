using Coworking.API.Infrastructure.Extensions;

namespace Coworking.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();

        services.ConfigureErrorHandling();

        services.AddCors(configuration);

        //services.AddHttpContextAccessor();

        //services.AddServices(configuration);
        services.AddApiServices();



        return services;
    }
}
