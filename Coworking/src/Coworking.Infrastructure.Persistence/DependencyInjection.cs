using Coworking.Application.Common.Interfaces;
using Coworking.Application.Common.Interfaces.Transactions;
using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Infrastructure.Persistence.Interceptors;
using Coworking.Infrastructure.Persistence.Transactions.Conflicts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TrackEntityInterceptor>();
            services.AddSingleton<IDbConflictDetector, PostgresConflictDetector>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var auditInterceptor = sp.GetRequiredService<TrackEntityInterceptor>();

                options
                    .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(auditInterceptor);
            });

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }

    }
}
