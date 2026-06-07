using Coworking.Application.Abstractions.Email;
using Coworking.Infrastructure.Services.Email.Options;
using Coworking.Infrastructure.Services.Email.Senders.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace Coworking.Infrastructure.Services.Email.Senders;

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ISmtpConnectionLimiter _connectionLimiter;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpOptions> options,
        ISmtpConnectionLimiter connectionLimiter,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionLimiter = connectionLimiter ?? throw new ArgumentNullException(nameof(connectionLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

#if DEBUG
        ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
#endif
    }

    public async Task SendRawEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken ct = default)
    {
        ValidateParameters(to, subject, body);

        var message = BuildMessage(to, subject, body);

        await using var _ = await _connectionLimiter.AcquireAsync(ct);

        try
        {
            await ExecuteSmtpOperationAsync(async client =>
            {
                await client.SendAsync(message, ct);

                _logger.LogTrace("Email sent to {Recipient}", to);
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw;
        }
    }

    private void ValidateParameters(string to, string subject, string body)
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
    {
        //return Regex.Replace(html, "<.*?>", string.Empty)
        //    .Replace("&nbsp;", " ")
        //    .Trim();

        return HtmlTextHelper.ToPlainText(html);
    }
}