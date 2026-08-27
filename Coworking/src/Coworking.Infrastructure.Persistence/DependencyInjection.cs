using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Transactions;
using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Infrastructure.Persistence.Interceptors;
using Coworking.Infrastructure.Persistence.Transactions.Conflicts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetServices<IInterceptor>());

            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                options
                    .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}