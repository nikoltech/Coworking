using Coworking.Application.Common.Interfaces;
using Coworking.Application.Common.Synchronization;
using Coworking.Infrastructure.Repositories;
using Coworking.Infrastructure.Synchronization;
using Coworking.Infrastructure.Synchronization.Background;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddSingleton<InMemoryBookingSynchronizer>();
        services.AddSingleton<IBookingSynchronizer>(sp =>
            sp.GetRequiredService<InMemoryBookingSynchronizer>());

        services.AddHostedService<ActiveRangeCleaner>();

        return services;
    }
}
