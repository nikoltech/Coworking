using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Email;
using Coworking.Application.Abstractions.Languages;
using Coworking.Application.Abstractions.Synchronization;
using Coworking.Application.Behaviors.Performance;
using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.Services.SlotGenerator;
using Coworking.External.Squidex;
using Coworking.Infrastructure.External.Squidex;
using Coworking.Infrastructure.Providers;
using Coworking.Infrastructure.Repositories;
using Coworking.Infrastructure.Services.Email.Messaging;
using Coworking.Infrastructure.Services.Email.Messaging.Interfaces;
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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions(configuration)
            .AddRepositories()
            .AddServices()
            .AddDomainServices()
            .AddSynchronization()
            .AddEmailMessaging(configuration);

        //services
        //    .AddSquidex(configuration)
        //    .AddSquidexContexts();

        services.AddHostedServices();

        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<PerformanceSettings>(configuration.GetSection("MediatR:Performance"));

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<IBookingRepository, BookingRepository>()
            .AddScoped<ICoworkingRepository, CoworkingRepository>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ILanguageProvider, LanguageProvider>();

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IBookingRoundingPolicy, DefaultRoundingPolicy>()
            .AddSingleton<ISlotGenerator, SlotGenerator>();

        return services;
    }

    private static IServiceCollection AddSynchronization(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryBookingAccessCoordinator>();
        services.AddSingleton<IBookingAccessCoordinator>(sp =>
            sp.GetRequiredService<InMemoryBookingAccessCoordinator>());

        return services;
    }

    private static IServiceCollection AddEmailMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        //services.AddMemoryCache();
        services.AddLazyCache();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection($"Services:{SmtpOptions.SectionName}"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<EmailChannel>();
        services.AddSingleton<IEmailChannel>(sp => sp.GetRequiredService<EmailChannel>());

        services
            .AddSingleton<IEmailTemplateService, EmailTemplateService>()
            .AddScoped<IEmailNotificationService, DirectEmailNotificationService>()
            .AddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private static IServiceCollection AddHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<BookingLockExpiryCleaner>();
        //services.AddHostedService<EmailBackgroundWorker>(); // replaced by RabbitMQ consumer + DirectEmailNotificationService

        return services;
    }
}