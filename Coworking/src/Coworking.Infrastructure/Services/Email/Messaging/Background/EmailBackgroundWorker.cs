using Coworking.Application.Notifications.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Coworking.Infrastructure.Services.Email.Messaging.Background;

public sealed class EmailBackgroundWorker(
    EmailChannel emailChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailBackgroundWorker> logger) : BackgroundService
{
    private const int MaxRetryAttempts = 3;

    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
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
                    "Email send attempt {Attempt}/{Max} failed. Retrying in {Delay}s.",
                    args.AttemptNumber + 1,
                    MaxRetryAttempts,
                    args.RetryDelay.TotalSeconds);

                return ValueTask.CompletedTask;
            }
        })
        .Build();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var email in emailChannel.Reader.ReadAllAsync(ct))
        {
            await SendWithRetryAsync(email, ct);
        }
    }

    private async Task SendWithRetryAsync(EmailNotification email, CancellationToken ct)
    {
        try
        {
            await _retryPipeline.ExecuteAsync(async token =>
            {
                using var scope = scopeFactory.CreateScope();
                await SendEmailAsync(email, scope, token);
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Email to {To} failed after all retry attempts. Subject: {Subject}",
                email.To, email.Subject);
        }
    }

    private async Task SendEmailAsync(
        EmailNotification email, IServiceScope scope, CancellationToken ct)
    {
        // TODO: resolve IEmailSender from scope and send
        throw new NotImplementedException();
    }
}