namespace Coworking.External.Squidex.Auth;

public interface ISquidexTokenService
{
    Task<string> GetTokenAsync(string clientName, CancellationToken ct);
    void InvalidateToken(string clientName);
}