using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Messaging.Consumers;
using Coworking.Messaging.Definitions;
using Coworking.Messaging.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace Coworking.Messaging;

public static class DependencyInjection
{
    /// <summary>
    /// Registers both publishers and consumers in a single MassTransit bus.
    /// Use in a monolith or during local development.
    /// For split deployments call <see cref="AddMessagingPublishers"/> / <see cref="AddMessagingConsumers"/> separately.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMqOptions(configuration);

        // Publishers are MediatR notification handlers.
        // AddMediatR can be called multiple times — registrations are merged.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddMassTransit(x =>
        {
            x.ConfigureConsumers();

            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();

                // Routes all Publish/Send calls through the Outbox.
                o.UseBusOutbox();
            });

            x.UsingRabbitMq(ConfigureRabbitMq);
        });

        return services;
    }

    /// <summary>
    /// MediatR publishers + Outbox + RabbitMQ bus (no consumers).
    /// Use in the service that owns the DB and the domain transactions.
    /// </summary>
    public static IServiceCollection AddMessagingPublishers(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMqOptions(configuration);

        // Publishers are MediatR notification handlers.
        // AddMediatR can be called multiple times — registrations are merged.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();

                // Routes all Publish/Send calls through the Outbox.
                o.UseBusOutbox();
            });

            x.UsingRabbitMq(ConfigureRabbitMq);
        });

        return services;
    }

    /// <summary>
    /// RabbitMQ consumers + retry + Inbox (no bus Outbox, no MediatR).
    /// Use in a dedicated notification service. The Inbox shares the host's AppDbContext,
    /// so the service must also call AddPersistence.
    /// </summary>
    public static IServiceCollection AddMessagingConsumers(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMqOptions(configuration);

        services.AddMassTransit(x =>
        {
            x.ConfigureConsumers();

            // Inbox only: this service consumes, it does not publish.
            x.AddEntityFrameworkOutbox<AppDbContext>(o => o.UsePostgres());

            x.UsingRabbitMq(ConfigureRabbitMq);
        });

        return services;
    }

    private static IServiceCollection AddRabbitMqOptions(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static void ConfigureRabbitMq(IBusRegistrationContext ctx,
        IRabbitMqBusFactoryConfigurator cfg)
    {
        var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        // MassTransit 8.x has no Host(host, port, vhost, configure) overload.
        // Port is set via URI only: rabbitmq://host:port/virtualHost.
        var uri = new Uri($"rabbitmq://{options.Host}:{options.Port}/{options.VirtualHost.TrimStart('/')}");

        cfg.Host(uri, h =>
        {
            h.Username(options.Username);
            h.Password(options.Password);
        });

        cfg.ConfigureEndpoints(ctx);
    }

    private static void ConfigureConsumers(this IBusRegistrationConfigurator x)
    {
        x.AddConsumer<BookingCreatedConsumer, EmailConsumerDefinition<BookingCreatedConsumer>>();
        x.AddConsumer<BookingCancelledConsumer, EmailConsumerDefinition<BookingCancelledConsumer>>();

        x.AddConsumer<BrokerTestConsumerA>();
        x.AddConsumer<BrokerTestConsumerB>();
    }
}
