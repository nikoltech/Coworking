using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Messaging;
using Coworking.Application.Abstractions.Synchronization;
using Coworking.Application.Notifications.Email;
using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.Services.SlotGenerator;
using Coworking.Infrastructure.Repositories;
using Coworking.Infrastructure.Services.Email.Messaging;
using Coworking.Infrastructure.Services.Email.Messaging.Background;
using Coworking.Infrastructure.Services.Email.Options;
using Coworking.Infrastructure.Services.Email.Senders;
using Coworking.Infrastructure.Services.Email.Services;
using Coworking.Infrastructure.Synchronization.InMemory;
using Coworking.Infrastructure.Synchronization.InMemory.Background;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddRepositories()
            .AddDomainServices()
            .AddSynchronization()
            .AddEmailMessaging(configuration);

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICoworkingRepository, CoworkingRepository>();

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IBookingRoundingPolicy, DefaultRoundingPolicy>();
        services.AddSingleton<ISlotGenerator, SlotGenerator>();

        return services;
    }

    private static IServiceCollection AddSynchronization(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryBookingAccessCoordinator>();
        services.AddSingleton<IBookingAccessCoordinator>(sp =>
            sp.GetRequiredService<InMemoryBookingAccessCoordinator>());

        services.AddHostedService<BookingLockExpiryCleaner>();

        return services;
    }

    private static IServiceCollection AddEmailMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection($"Services:{SmtpOptions.SectionName}"))
            //.ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<EmailChannel>();
        services.AddSingleton<IEmailChannel>(sp => sp.GetRequiredService<EmailChannel>());

        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        services.AddTransient<IEmailSender, SmtpEmailSender>();

        services.AddHostedService<EmailBackgroundWorker>();

        return services;
    }
}