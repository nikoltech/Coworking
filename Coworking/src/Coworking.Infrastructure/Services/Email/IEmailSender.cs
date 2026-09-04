namespace Coworking.Infrastructure.Services.Email;

public interface IEmailSender
{
    Task SendRawEmailAsync(string to, string subject, string body, CancellationToken ct);
}