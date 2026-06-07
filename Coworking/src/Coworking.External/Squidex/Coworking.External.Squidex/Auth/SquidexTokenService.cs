using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Auth;

/// <summary>
/// Fetches and caches OAuth2 tokens per app+client combination.
/// Cache key format: "{appName}:{clientName}"
/// Thread-safe — SemaphoreSlim prevents token stampede.
/// </summary>
public sealed class SquidexTokenService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<SquidexGlobalOptions> options)
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SquidexGlobalOptions _options = options.Value;

    public async Task<string> GetTokenAsync(
        string appName, string clientName,
        CancellationToken ct)
    {
        var cacheKey = CacheKey(appName, clientName);

        if (cache.TryGetValue(cacheKey, out string? token) && token is not null)
            return token;

        await _gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(cacheKey, out token) && token is not null)
                return token;

            var credentials = GetCredentials(appName, clientName);
            var appOptions = GetAppOptions(appName);
            var result = await RequestTokenAsync(appOptions.BaseUrl, credentials, ct);
            var expiry = TimeSpan.FromSeconds(result.ExpiresIn) - ExpiryBuffer;

            cache.Set(cacheKey, result.AccessToken, expiry);
            return result.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InvalidateToken(string appName, string clientName) =>
        cache.Remove(CacheKey(appName, clientName));

    // ── private ──────────────────────────────────────────────────────────────

    private SquidexAppOptions GetAppOptions(string appName) =>
        _options.Apps.TryGetValue(appName, out var app)
            ? app
            : throw new InvalidOperationException(
                $"Squidex app '{appName}' is not configured. " +
                $"Available: {string.Join(", ", _options.Apps.Keys)}");

    private SquidexClientCredentials GetCredentials(string appName, string clientName)
    {
        var app = GetAppOptions(appName);

        return app.Clients.TryGetValue(clientName, out var creds)
            ? creds
            : throw new InvalidOperationException(
                $"Squidex client '{clientName}' is not configured for app '{appName}'. " +
                $"Available: {string.Join(", ", app.Clients.Keys)}");
    }

    private async Task<TokenResponse> RequestTokenAsync(
        string baseUrl, SquidexClientCredentials credentials,
        CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient(SquidexHttpClientNames.Auth);
        var url = $"{baseUrl.TrimEnd('/')}/identity-server/connect/token";

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = credentials.ClientId,
            ["client_secret"] = credentials.ClientSecret,
            ["grant_type"] = "client_credentials",
            ["scope"] = "squidex-api"
        });

        var response = await http.PostAsync(url, body, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
               ?? throw new InvalidOperationException("Empty token response from Squidex.");
    }

    private static string CacheKey(string appName, string clientName) =>
        $"squidex:token:{appName}:{clientName}";

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn);
}