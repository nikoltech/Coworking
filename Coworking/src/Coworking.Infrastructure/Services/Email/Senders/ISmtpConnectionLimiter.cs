namespace Coworking.Infrastructure.Services.Email.Senders;

internal interface ISmtpConnectionLimiter
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken ct);
}
