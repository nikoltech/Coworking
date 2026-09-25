using Coworking.Application.Ports;
using Coworking.Application.Ports.Transactions;
using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Infrastructure.Persistence.Interceptors;
using Coworking.Infrastructure.Persistence.Transactions.Conflicts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Coworking.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddSingleton<IInterceptor, TrackEntityInterceptor>()
            .AddSingleton<IInterceptor, BookingTimeInterceptor>()
            .AddSingleton<IDbConflictDetector, PostgresConflictDetector>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options
                .UseAppDatabase(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(sp.GetServices<IInterceptor>());

            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                options
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}