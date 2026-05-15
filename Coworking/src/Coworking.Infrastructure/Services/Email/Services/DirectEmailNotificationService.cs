using Coworking.Application.Abstractions.Email;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;
using Coworking.Infrastructure.Services.Email.Templates.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Mail;

namespace Coworking.Infrastructure.Services.Email.Services;

/// <summary>
/// Sends emails directly inside the caller's execution context (no Channel, no background worker).
/// Designed for use in RabbitMQ consumers where retry is already handled at the transport level,
/// but we still want Polly as a safety net for transient SMTP failures.
/// </summary>
internal sealed class DirectEmailNotificationService(
    IEmailTemplateService templateService,
    IEmailSender emailSender,
    ILogger<DirectEmailNotificationService> logger) : IEmailNotificationService
{
    private const int MaxRetryAttempts = 3;

    private readonly ResiliencePipeline _retryPipeline = BuildRetryPipeline(logger);

    public async Task SendBookingCreatedAsync(BookingCreatedEmailModel model, CancellationToken ct)
    {
        var body = await templateService.RenderTemplateFromHbsFileAsync(
            "booking-created.hbs",
            new BookingCreatedTemplateModel(
                To: model.To,
                UserName: model.UserName,
                DeskName: model.DeskName,
                CoworkingName: model.CoworkingName,
                FormattedStart: model.FormattedStart,
                FormattedEnd: model.FormattedEnd,
                TimeZoneId: model.TimeZoneId));

        var subject = $"Booking created — {model.CoworkingName}. Waiting for payment confirmation.";

        await SendWithRetryAsync(model.To, subject, body, ct);
    }

    public async Task SendBookingCancelledAsync(BookingCancelledEmailModel model, CancellationToken ct)
    {
        var body = await templateService.RenderTemplateFromHbsFileAsync(
            "booking-cancelled.hbs",
            new BookingCancelledTemplateModel(
                To: model.To,
                UserName: model.UserName,
                DeskName: model.DeskName,
                CoworkingName: model.CoworkingName,
                FormattedStart: model.FormattedStart,
                FormattedEnd: model.FormattedEnd,
                TimeZoneId: model.TimeZoneId,
                CancellationReason: model.CancellationReason));

        var subject = $"Booking cancelled — {model.CoworkingName}";

        await SendWithRetryAsync(model.To, subject, body, ct);
    }

    /** private **********************/

    private async Task SendWithRetryAsync(string to, string subject, string body, CancellationToken ct)
    {
        await _retryPipeline.ExecuteAsync(async token =>
            await emailSender.SendRawEmailAsync(to, subject, body, token), ct);

        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("Email sent to {To}. Subject: {Subject}", to, subject);
    }

    /** helpers **********************/

    private static ResiliencePipeline BuildRetryPipeline(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(3),
                MaxDelay = TimeSpan.FromSeconds(30),

                ShouldHandle = args =>
                    ValueTask.FromResult(
                        args.Outcome.Exception is not null &&
                        IsTransient(args.Outcome.Exception)),

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

    private static bool IsTransient(Exception ex) =>
        ex switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            SmtpException smtpEx when IsTransientSmtpCode(smtpEx) => true,
            _ => false
        };

    private static bool IsTransientSmtpCode(SmtpException ex) =>
        ex.StatusCode switch
        {
            SmtpStatusCode.GeneralFailure => true,
            SmtpStatusCode.MailboxBusy => true,
            SmtpStatusCode.ServiceNotAvailable => true,
            SmtpStatusCode.TransactionFailed => true,
            SmtpStatusCode.ExceededStorageAllocation => true,

            // ❌ permanent
            SmtpStatusCode.MailboxUnavailable => false,
            SmtpStatusCode.UserNotLocalTryAlternatePath => false,
            SmtpStatusCode.MailboxNameNotAllowed => false,

            _ => false
        };
}
