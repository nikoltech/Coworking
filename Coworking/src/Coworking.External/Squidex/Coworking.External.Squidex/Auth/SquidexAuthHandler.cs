using Coworking.External.Squidex.Client;
using System.Net;
using System.Net.Http.Headers;

namespace Coworking.External.Squidex.Auth;

/// <summary>
/// Attaches Bearer token to every Squidex request.
/// On 401 — invalidates cached token and retries once.
/// App name and client name are passed via HttpRequestMessage.Options.
/// </summary>
public sealed class SquidexAuthHandler(SquidexTokenService tokenService) : DelegatingHandler
{
    public const string AppNameKey = "SquidexAppName";
    public const string ClientNameKey = "SquidexClientName";
    public const string DefaultClient = "Default";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var appName = GetOption(request, AppNameKey) ?? string.Empty;
        var clientName = GetOption(request, ClientNameKey) ?? DefaultClient;

        request.Headers.Authorization = await GetAuthHeaderAsync(appName, clientName, ct);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        tokenService.InvalidateToken(appName, clientName);
        response.Dispose();

        var retryRequest = await request.CloneAsync(ct);
        retryRequest.Headers.Authorization = await GetAuthHeaderAsync(appName, clientName, ct);

        return await base.SendAsync(retryRequest, ct);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync(
        string appName, string clientName,
        CancellationToken ct)
    {
        var token = await tokenService.GetTokenAsync(appName, clientName, ct);
        return new AuthenticationHeaderValue("Bearer", token);
    }

    private static string? GetOption(HttpRequestMessage request, string key) =>
        request.Options.TryGetValue(
            new HttpRequestOptionsKey<string>(key), out var value)
            ? value
            : null;
}