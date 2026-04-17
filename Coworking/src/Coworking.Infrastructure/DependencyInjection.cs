using Coworking.Application.Common.Interfaces;
using Coworking.Application.Common.Synchronization;
using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.Services.SlotGenerator;
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
        services.AddSingleton<ISlotGenerator, SlotGenerator>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICoworkingRepository, CoworkingRepository>();

        services.AddSingleton<InMemoryBookingAccessCoordinator>();
        services.AddSingleton<IBookingAccessCoordinator>(sp =>
            sp.GetRequiredService<InMemoryBookingAccessCoordinator>());

        services.AddHostedService<BookingLockExpiryCleaner>();

        return services;
    }
}
