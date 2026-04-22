using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Transactions;
using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Infrastructure.Persistence.Interceptors;
using Coworking.Infrastructure.Persistence.Transactions.Conflicts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddSingleton<TrackEntityInterceptor>()
            .AddSingleton<IDbConflictDetector, PostgresConflictDetector>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<TrackEntityInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}