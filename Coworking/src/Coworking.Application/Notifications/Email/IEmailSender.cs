namespace Coworking.Application.Notifications.Email;

public interface IEmailSender
{
    Task SendRawEmailAsync(string to, string subject, string body, CancellationToken ct);
}