using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Messaging.Consumers;
using Coworking.Messaging.Options;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Coworking.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Регистрируем Publishers как MediatR-обработчики нотификаций.
        // AddMediatR можно вызывать несколько раз — регистрации мержатся.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddMassTransit(x =>
        {
            x.ConfigureConsumers();

            // Outbox: Publish() записывает сообщение в БД в рамках текущей транзакции.
            // Фоновый воркер MassTransit читает таблицу OutboxMessage и доставляет в RabbitMQ.
            // Гарантия: если сервис упал после Commit() — сообщение не потеряется,
            // воркер доставит его при следующем старте.
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();

                // Все Publish/Send автоматически проходят через Outbox.
                // Без этой строки Outbox работает только для консьюмеров (InboxState).
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                // MassTransit 8.x не имеет перегрузки Host(host, port, vhost, configure).
                // Порт задаётся только через URI: rabbitmq://host:port/virtualHost.
                var uri = new Uri($"rabbitmq://{options.Host}:{options.Port}/{options.VirtualHost.TrimStart('/')}");

                cfg.Host(uri, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }

    private static void ConfigureConsumers(this IBusRegistrationConfigurator x)
    {
        // Retry настраивается per-consumer, а не глобально.
        // Глобальный retry опасен: permanent ошибки (баг в данных, ArgumentException)
        // будут повторяться N раз с тем же результатом и лишь задержат попадание в DLQ.

        x.AddConsumer<BookingCreatedConsumer>(c => c.UseMessageRetry(ConfigureRetry));
        x.AddConsumer<BookingCancelledConsumer>(c => c.UseMessageRetry(ConfigureRetry));
    }

    // Общая политика retry для всех консьюмеров.
    // Повторяем только transient ошибки (сеть, таймаут).
    // Permanent ошибки (баг в данных) — сразу в DLQ без лишних попыток.
    private static void ConfigureRetry(IRetryConfigurator r)
    {
        r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
        r.Ignore<ArgumentException>(); // immediate in DLQ, like default
        r.Ignore<InvalidOperationException>();
        r.Handle<HttpRequestException>();
        r.Handle<TimeoutException>();
        r.Handle<SmtpException>();
    }
}
