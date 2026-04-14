using Coworking.Application.Common.Interfaces;
using Coworking.Application.Common.Synchronization;
using Coworking.Domain.Policies.Rounding;
using Coworking.Infrastructure.Repositories;
using Coworking.Infrastructure.Synchronization.InMemory;
using Coworking.Infrastructure.Synchronization.InMemory.Background;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IBookingRoundingPolicy, DefaultRoundingPolicy>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICoworkingRepository, CoworkingRepository>();

        services.AddSingleton<InMemoryBookingOverlapGate>();
        services.AddSingleton<IBookingOverlapGate>(sp =>
            sp.GetRequiredService<InMemoryBookingOverlapGate>());

        services.AddHostedService<BookingLockExpiryCleaner>();

        return services;
    }
}
