using System.Net;
using System.Net.Http.Headers;

namespace Coworking.External.Squidex.Auth;

/// <summary>
/// Attaches Bearer token to every Squidex HTTP request.
/// On 401 — invalidates cached token and retries once with a fresh token.
/// Client name is passed via HttpRequestMessage.Options.
/// </summary>
public sealed class SquidexAuthHandler(ISquidexTokenService tokenService) : DelegatingHandler
{
    public const string ClientNameKey = "SquidexClientName";
    public const string DefaultClient = "Default";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var clientName = GetClientName(request);

        request.Headers.Authorization = await GetAuthHeaderAsync(clientName, ct);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Token expired mid-flight — refresh and retry once
        tokenService.InvalidateToken(clientName);
        response.Dispose();

        var retryRequest = await CloneAsync(request, ct);
        retryRequest.Headers.Authorization = await GetAuthHeaderAsync(clientName, ct);

        return await base.SendAsync(retryRequest, ct);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync(
        string clientName, CancellationToken ct)
    {
        var token = await tokenService.GetTokenAsync(clientName, ct);
        return new AuthenticationHeaderValue("Bearer", token);
    }

    private static string GetClientName(HttpRequestMessage request) =>
        request.Options.TryGetValue(
            new HttpRequestOptionsKey<string>(ClientNameKey), out var name)
            ? name ?? DefaultClient
            : DefaultClient;

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage source, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);

        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in source.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        if (source.Content is null)
            return clone;

        var bytes = await source.Content.ReadAsByteArrayAsync(ct);
        clone.Content = new ByteArrayContent(bytes);

        foreach (var header in source.Content.Headers)
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}