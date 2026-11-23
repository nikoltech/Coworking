namespace Coworking.External.Squidex.Auth;

public interface ISquidexTokenService
{
    Task<string> GetTokenAsync(string appName, string clientName, CancellationToken ct);
    void InvalidateToken(string appName, string clientName);
}
