using Coworking.Infrastructure.Services.Email;
using MassTransit;

namespace Coworking.Messaging;

internal static class ConsumerPipelines
{
    public static void Email<T>(IConsumerConfigurator<T> c) where T : class
    {
        // Tier 1: delayed redelivery — releases the consumer slot, handles all transient failures.
        c.UseDelayedRedelivery(r =>
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
        c.UseMessageRetry(r =>
        {
            r.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300));

            r.Handle<EmailConnectionException>();
        });
    }
}
