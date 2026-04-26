using Coworking.API.Infrastructure.Swagger;

namespace Coworking.API.Infrastructure.Extensions
{
    internal static class ServicesExtensions
    {
        internal static void AddApiServices(this IServiceCollection services)
        {
            services.AddSwagger();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(DependencyInjection).Assembly);
            });

        }
    }
}
