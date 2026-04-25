using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Auth;

/// <summary>
/// Fetches and caches OAuth2 tokens per Squidex client.
/// Thread-safe — SemaphoreSlim prevents token stampede under concurrent load.
/// </summary>
public sealed class SquidexTokenService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<SquidexOptions> options)
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SquidexOptions _options = options.Value;

    public async Task<string> GetTokenAsync(string clientName, CancellationToken ct)
    {
        var cacheKey = $"squidex:token:{clientName}";

        if (cache.TryGetValue(cacheKey, out string? token) && token is not null)
            return token;

        await _gate.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the gate
            if (cache.TryGetValue(cacheKey, out token) && token is not null)
                return token;

            var credentials = GetCredentials(clientName);
            var result = await RequestTokenAsync(credentials, ct);
            var expiry = TimeSpan.FromSeconds(result.ExpiresIn) - ExpiryBuffer;

            cache.Set(cacheKey, result.AccessToken, expiry);
            return result.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InvalidateToken(string clientName) =>
        cache.Remove($"squidex:token:{clientName}");

    // ── private ──────────────────────────────────────────────────────────────

    private SquidexClientCredentials GetCredentials(string clientName) =>
        _options.Clients.TryGetValue(clientName, out var credentials)
            ? credentials
            : throw new InvalidOperationException(
                $"Squidex client '{clientName}' is not configured. " +
                $"Available: {string.Join(", ", _options.Clients.Keys)}");

    private async Task<TokenResponse> RequestTokenAsync(
        SquidexClientCredentials credentials, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient(SquidexHttpClientNames.Auth);
        var url = $"{_options.BaseUrl.TrimEnd('/')}/identity-server/connect/token";

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

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn);
}