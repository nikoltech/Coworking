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
    internal const string AppNameKey = "SquidexAppName";
    internal const string ClientNameKey = "SquidexClientName";
    internal const string DefaultClient = "Default";

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

        var retryRequest = await CloneAsync(request, ct);
        retryRequest.Headers.Authorization = await GetAuthHeaderAsync(appName, clientName, ct);

        return await base.SendAsync(retryRequest, ct);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync(
        string appName, string clientName, CancellationToken ct)
    {
        var token = await tokenService.GetTokenAsync(appName, clientName, ct);
        return new AuthenticationHeaderValue("Bearer", token);
    }

    private static string? GetOption(HttpRequestMessage request, string key) =>
        request.Options.TryGetValue(
            new HttpRequestOptionsKey<string>(key), out var value)
            ? value
            : null;

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