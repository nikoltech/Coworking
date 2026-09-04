using Coworking.Infrastructure.Services.Email.Options;
using Coworking.Infrastructure.Services.Email.Senders.Helpers;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Sockets;

namespace Coworking.Infrastructure.Services.Email.Senders;

internal sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ISmtpConnectionLimiter connectionLimiter,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendRawEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken ct = default)
    {
        ValidateParameters(to, subject, body);

        var message = BuildMessage(to, subject, body);

        try
        {
            await using var _ = await connectionLimiter.AcquireAsync(ct);

            await ExecuteSmtpOperationAsync(async client =>
            {
                await client.SendAsync(message, ct);

                if (logger.IsEnabled(LogLevel.Trace) && logger.IsEnabled(LogLevel.Debug))
                    logger.LogTrace("Email sent to {Recipient}", to);
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipient}", to);

            throw Translate(ex, to);
        }
    }

    internal static Exception Translate(Exception ex, string to) =>
        ex switch
        {
            EmailDeliveryException => ex,

            SmtpCommandException command => IsTransientCode(command.StatusCode)
                ? new EmailTransientException($"SMTP server temporarily rejected {to}: {command.StatusCode}.", ex)
                : new EmailPermanentException($"SMTP server rejected {to}: {command.StatusCode}.", ex),

            SmtpProtocolException or SocketException or IOException or TimeoutException =>
                new EmailConnectionException($"Connection to the SMTP server failed for {to}.", ex),

            AuthenticationException or ServiceNotAuthenticatedException =>
                new EmailPermanentException($"SMTP authentication failed while sending to {to}.", ex),

            _ => new EmailPermanentException($"Sending to {to} failed: {ex.GetType().Name}.", ex)
        };

    private static bool IsTransientCode(SmtpStatusCode statusCode) =>
        (int)statusCode is >= 400 and < 500;

    private static void ValidateParameters(string to, string subject, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
    }

    private MimeMessage BuildMessage(string to, string subject, string body)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = StripHtml(body)
        }.ToMessageBody();

        return message;
    }

    private async Task ExecuteSmtpOperationAsync(
        Func<SmtpClient, Task> operation,
        CancellationToken ct)
    {
        using var client = new SmtpClient();

#if DEBUG
        // per client: MailKit validates through SslStream, which ServicePointManager never reached
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;
#endif

        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        try
        {
            await operation(client);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }

    private static string StripHtml(string html)
        => HtmlTextHelper.ToPlainText(html);
}