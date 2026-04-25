using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Client;

/// <summary>
/// Low-level Squidex REST client.
/// Handles HTTP operations, headers, serialization and transient retries.
/// Auth is handled by SquidexAuthHandler in the HTTP pipeline.
/// </summary>
public sealed class SquidexApiClient
{
    private readonly HttpClient _http;
    private readonly SquidexOptions _options;
    private readonly string _clientName;
    private readonly SquidexLocaleProvider _locales;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SquidexApiClient(
        HttpClient http,
        SquidexOptions options,
        string clientName,
        SquidexLocaleProvider locales)
    {
        _http = http;
        _options = options;
        _clientName = clientName;
        _locales = locales;
    }

    // ── Content ──────────────────────────────────────────────────────────────

    public Task<ResponseSchema<T>> QueryAsync<T>(
        string schema,
        RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(
            HttpMethod.Get,
            $"{ContentUrl(schema)}?{ToQueryString(query)}",
            queryOptions);

        return SendAndDeserializeAsync<ResponseSchema<T>>(request, ct);
    }

    public async Task<ContentDto<T>?> GetByIdAsync<T>(
        string schema,
        string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Get, $"{ContentUrl(schema)}/{id}", queryOptions);
        var response = await SendWithRetryAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await response.EnsureSquidexSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<ContentDto<T>>(Json, ct);
    }

    public Task<ContentDto<T>> CreateAsync<T>(
        string schema, T data, bool publish = true, CancellationToken ct = default)
    {
        var url = ContentUrl(schema) + (publish ? "?publish=true" : string.Empty);
        var request = BuildRequest(HttpMethod.Post, url);
        request.Content = JsonContent.Create(data, options: Json);
        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    public Task<ContentDto<T>> UpdateAsync<T>(
        string schema, string id, T data, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Put, $"{ContentUrl(schema)}/{id}");
        request.Content = JsonContent.Create(data, options: Json);
        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    public Task<ContentDto<T>> PatchAsync<T>(
        string schema, string id, T data, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Patch, $"{ContentUrl(schema)}/{id}");
        request.Content = JsonContent.Create(data, options: Json);
        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    public async Task DeleteAsync(
        string schema, string id, bool permanent = false, CancellationToken ct = default)
    {
        var url = $"{ContentUrl(schema)}/{id}" + (permanent ? "?permanent=true" : string.Empty);
        var request = BuildRequest(HttpMethod.Delete, url);
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);
    }

    public Task<ContentDto<T>> ChangeStatusAsync<T>(
        string schema, string id, string newStatus, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Put, $"{ContentUrl(schema)}/{id}/status");
        request.Content = JsonContent.Create(new { status = newStatus }, options: Json);
        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    // ── App ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetAppLocalesAsync(CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/apps/{_options.AppName}/languages";
        var request = BuildRequest(HttpMethod.Get, url);
        var response = await SendWithRetryAsync(request, ct);

        await response.EnsureSquidexSuccessAsync(ct);

        var result = await response.Content
            .ReadFromJsonAsync<AppLanguagesResponse>(Json, ct);

        return result?.Items.Select(l => l.Iso2Code).ToList() ?? [];
    }

    // ── Retry ────────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var requestToSend = attempt == 1
                ? request
                : await CloneAsync(request, ct);

            var response = await _http.SendAsync(requestToSend, ct);

            if (!IsTransient(response.StatusCode) || attempt == maxAttempts)
                return response;

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), ct);
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

    // ── Private ──────────────────────────────────────────────────────────────

    private async Task<T> SendAndDeserializeAsync<T>(
        HttpRequestMessage request, CancellationToken ct)
    {
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new InvalidOperationException(
                   $"Empty Squidex response for {typeof(T).Name}.");
    }

    private HttpRequestMessage BuildRequest(
        HttpMethod method,
        string url,
        QueryOptions? queryOptions = null)
    {
        var request = new HttpRequestMessage(method, url);
        var opts = queryOptions ?? QueryOptions.Default;

        request.Options.Set(
            new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey),
            _clientName);

        if (opts.IncludeUnpublished)
            request.Headers.Add("X-Unpublished", "true");

        if (opts.NoSlowTotal)
            request.Headers.Add("X-NoSlowTotal", "true");

        if (opts.Flatten)
        {
            // Flatten to scalar — schema DTO must use plain C# types
            request.Headers.Add("X-Flatten", "true");

            var languages = opts.Languages ?? [_locales.DefaultLocale];
            request.Headers.Add("X-Languages", string.Join(",", languages));
        }
        else
        {
            // Return only requested locales — avoids pulling all locales per field
            var languages = opts.Languages ?? _locales.SupportedLocales;
            if (languages.Count > 0)
                request.Headers.Add("X-Languages", string.Join(",", languages));
        }

        return request;
    }

    private static string ToQueryString(RequestQuery query)
    {
        var json = JsonSerializer.Serialize(query, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        return "q=" + Uri.EscapeDataString(json);
    }

    private string ContentUrl(string schema) =>
        $"{_options.BaseUrl.TrimEnd('/')}/api/content/{_options.AppName}/{schema}";

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

    // ── Response types ────────────────────────────────────────────────────────

    private sealed record AppLanguagesResponse(
        [property: JsonPropertyName("items")] List<AppLanguage> Items);

    private sealed record AppLanguage(
        [property: JsonPropertyName("iso2Code")] string Iso2Code);
}