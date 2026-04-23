using Coworking.API.Mappings;

namespace Coworking.API.Infrastructure.Extensions
{
    internal static class ServicesExtensions
    {
        internal static void AddApiServices(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(DependencyInjection).Assembly);
            });
        }
    }
}
