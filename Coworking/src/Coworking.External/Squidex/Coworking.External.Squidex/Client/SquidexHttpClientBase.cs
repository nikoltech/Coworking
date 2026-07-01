using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Client;

/// <summary>
/// Shared HTTP transport for Squidex clients: auth-key stamping, retry with exponential
/// backoff, and JSON deserialization.
/// <para>
/// API-specific concerns (endpoints, query-string building, headers, response shapes) live
/// in the derived clients — the Content and Assets APIs are similar but distinct.
/// </para>
/// </summary>
internal abstract class SquidexHttpClientBase
{
    private readonly HttpClient _http;
    private readonly string _clientName;

    /// <summary>Read + write options shared by all Squidex payloads.</summary>
    protected static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SquidexAppOptions AppOptions { get; }

    protected SquidexHttpClientBase(HttpClient http, SquidexAppOptions appOptions, string clientName)
    {
        _http = http;
        AppOptions = appOptions;
        _clientName = clientName;
    }

    // ── Request building ──────────────────────────────────────────────────────

    /// <summary>Creates a request stamped with the app + client so the auth handler can resolve a token.</summary>
    protected HttpRequestMessage CreateRequest(HttpMethod method, string url) =>
        Stamp(new HttpRequestMessage(method, url));

    /// <inheritdoc cref="CreateRequest(HttpMethod, string)"/>
    protected HttpRequestMessage CreateRequest(HttpMethod method, Uri uri) =>
        Stamp(new HttpRequestMessage(method, uri));

    private HttpRequestMessage Stamp(HttpRequestMessage request)
    {
        request.Options.Set(
            new HttpRequestOptionsKey<string>(SquidexAuthHandler.AppNameKey), AppOptions.AppName);
        request.Options.Set(
            new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey), _clientName);

        return request;
    }

    // ── Send ──────────────────────────────────────────────────────────────────

    protected async Task<T> SendAndDeserializeAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new InvalidOperationException(
                   $"Empty Squidex response for {typeof(T).Name}.");
    }

    protected async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var retry = AppOptions.Retry;

        for (var attempt = 1; attempt <= retry.MaxAttempts; attempt++)
        {
            var requestToSend = attempt == 1
                ? request
                : await request.CloneAsync(ct);

            var response = await _http.SendAsync(requestToSend, ct);

            if (!IsTransient(response.StatusCode) || attempt == retry.MaxAttempts)
                return response;

            response.Dispose();
            await Task.Delay(
                TimeSpan.FromSeconds(retry.BaseDelaySeconds * Math.Pow(2, attempt - 1)), ct);
        }

        throw new UnreachableException();
    }

    private static bool IsTransient(HttpStatusCode code) => code is
        HttpStatusCode.RequestTimeout or
        HttpStatusCode.TooManyRequests or
        HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout;
}
