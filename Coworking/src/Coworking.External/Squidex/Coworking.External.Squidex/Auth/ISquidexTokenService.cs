namespace Coworking.External.Squidex.Auth;

public interface ISquidexTokenService
{
    Task<string> GetTokenAsync(string appName, string clientName, CancellationToken ct);
    Task InvalidateTokenAsync(string appName, string clientName, string rejectedToken, CancellationToken ct);
}
