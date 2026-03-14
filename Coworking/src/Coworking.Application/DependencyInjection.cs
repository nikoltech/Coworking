using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Coworking.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper();
            services.AddMediatR();

            return services;
        }

        private static void AddAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(_ => { }, typeof(DependencyInjection).Assembly);
        }

        private static void AddMediatR(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        }
    }
}
