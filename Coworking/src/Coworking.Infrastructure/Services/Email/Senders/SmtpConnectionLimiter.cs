namespace Coworking.Infrastructure.Services.Email.Senders;

/// <summary>
/// Singleton semaphore that caps parallel SMTP connections across all SmtpEmailSender instances.
/// Configured via <see cref="Options.SmtpOptions.MaxConcurrentConnections"/>.
/// </summary>
internal sealed class SmtpConnectionLimiter(int maxConcurrentConnections) : ISmtpConnectionLimiter, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(maxConcurrentConnections, maxConcurrentConnections);

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        return new Slot(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Slot(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
