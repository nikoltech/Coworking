namespace Coworking.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendRawEmailAsync(string to, string subject, string body, CancellationToken ct);
}