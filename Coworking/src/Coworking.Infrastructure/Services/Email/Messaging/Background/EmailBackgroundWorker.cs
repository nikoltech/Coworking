using Coworking.Application.Abstractions.Email;
using Coworking.Infrastructure.Services.Email.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Coworking.Infrastructure.Services.Email.Messaging.Background;

/// <summary>
/// Processes outgoing emails in parallel, respecting SMTP connection limits.
/// Parallel degree is configured via <see cref="SmtpOptions.MaxConcurrentConnections"/>.
/// </summary>
public sealed class EmailBackgroundWorker(
    EmailChannel emailChannel,
    IServiceScopeFactory scopeFactory,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<EmailBackgroundWorker> logger) : BackgroundService
{
    private const int MaxRetryAttempts = 3;

    private readonly ResiliencePipeline _retryPipeline = BuildRetryPipeline(logger);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation(
            "Email worker started. Max parallel connections: {MaxConcurrentConnections}.",
            smtpOptions.Value.MaxConcurrentConnections);

        await Parallel.ForEachAsync(
            emailChannel.Reader.ReadAllAsync(ct),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = smtpOptions.Value.MaxConcurrentConnections,
                CancellationToken = ct
            },
            async (email, token) => await SendWithRetryAsync(email, token));
    }

    // ── private ──────────────────────────────────────────────────────────────

    private async Task SendWithRetryAsync(EmailNotification email, CancellationToken ct)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(async token =>
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                await sender.SendRawEmailAsync(email.To, email.Subject, email.Body, token);
            }, ct);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Email sent to {To}. Subject: {Subject}", email.To, email.Subject);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Email to {To} failed after {Max} attempts. Subject: {Subject}",
                email.To, MaxRetryAttempts, email.Subject);
        }
    }

    private static ResiliencePipeline BuildRetryPipeline(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(3),
                MaxDelay = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Attempt {Attempt}/{Max} failed. Retrying in {Delay}s.",
                        args.AttemptNumber + 1,
                        MaxRetryAttempts,
                        args.RetryDelay.TotalSeconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}