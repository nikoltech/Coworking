using MassTransit;
using System.Net.Mail;

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

            r.Handle<TimeoutException>();
            r.Handle<HttpRequestException>();
            r.Handle<SmtpException>(ex => IsTransientSmtpCode(ex));

            r.Ignore<SmtpException>(ex => !IsTransientSmtpCode(ex));
            r.Ignore<ArgumentException>();
            r.Ignore<InvalidOperationException>();
        });

        // Tier 2: fast in-place retry — for momentary network blips only (no SmtpException).
        c.UseMessageRetry(r =>
        {
            r.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500));
            r.Handle<TimeoutException>();
            r.Handle<HttpRequestException>();
        });
    }

    private static bool IsTransientSmtpCode(SmtpException ex) =>
        ex.StatusCode switch
        {
            SmtpStatusCode.GeneralFailure         => true,
            SmtpStatusCode.MailboxBusy            => true,
            SmtpStatusCode.ServiceNotAvailable    => true,
            SmtpStatusCode.TransactionFailed      => true,
            SmtpStatusCode.ExceededStorageAllocation => true,
            _                                     => false
        };
}
