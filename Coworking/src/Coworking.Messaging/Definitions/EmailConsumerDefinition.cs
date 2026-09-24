using Coworking.Infrastructure.Persistence.Contexts;
using Coworking.Infrastructure.Services.Email;
using MassTransit;

namespace Coworking.Messaging.Definitions;

/// <summary>
/// Retry policies are configured on the endpoint rather than on the consumer, so they stay
/// outside the inbox filter and every attempt opens its own transaction.
/// </summary>
internal sealed class EmailConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Tier 1: delayed redelivery — releases the consumer slot, handles all transient failures.
        endpointConfigurator.UseDelayedRedelivery(r =>
        {
            // 5 attempts: 10–300s each, total 50s – ~16 min
            r.Exponential(5, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(30));

            r.Handle<EmailTransientException>();

            r.Ignore<EmailPermanentException>();
            r.Ignore<ArgumentException>();
            r.Ignore<InvalidOperationException>();
        });

        // Tier 2: fast in-place retry — a server that asked us to wait must not be hammered,
        // so only failures that never reached it are repeated immediately.
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300));

            r.Handle<EmailConnectionException>();
        });

        // last, so the inbox transaction opens inside the retries
        endpointConfigurator.UseEntityFrameworkOutbox<AppDbContext>(context);
    }
}
