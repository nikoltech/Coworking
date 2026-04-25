using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Client;

/// <summary>
/// Squidex Assets API client.
/// Separate from SquidexApiClient — different endpoint, different response shape.
/// </summary>
public sealed class SquidexAssetClient
{
    private readonly HttpClient _http;
    private readonly SquidexOptions _options;
    private readonly string _clientName;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SquidexAssetClient(
        HttpClient http,
        SquidexOptions options,
        string clientName)
    {
        _http = http;
        _options = options;
        _clientName = clientName;
    }

    public Task<ResponseSchema<AssetDto>> QueryAsync(
        AssetQuery? query = null,
        CancellationToken ct = default)
    {
        var url = $"{AssetsUrl()}?{ToQueryString(query ?? AssetQuery.Create())}";
        var request = BuildRequest(HttpMethod.Get, url);
        return SendAndDeserializeAsync<ResponseSchema<AssetDto>>(request, ct);
    }

    public async Task<AssetDto?> GetByIdAsync(
        string id, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Get, $"{AssetsUrl()}/{id}");
        var response = await SendWithRetryAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await response.EnsureSquidexSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<AssetDto>(Json, ct);
    }

    public async Task<AssetDto> UploadAsync(
        Stream stream, string fileName, string mimeType, CancellationToken ct = default)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);

        var request = BuildRequest(HttpMethod.Post, AssetsUrl());
        request.Content = content;

        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<AssetDto>(Json, ct)
               ?? throw new InvalidOperationException("Empty response from Squidex Assets.");
    }

    public async Task<AssetDto> UpdateMetadataAsync(
        string id, UpdateAssetRequest update, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Put, $"{AssetsUrl()}/{id}");
        request.Content = JsonContent.Create(update, options: Json);

        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<AssetDto>(Json, ct)
               ?? throw new InvalidOperationException("Empty response from Squidex Assets.");
    }

    public async Task DeleteAsync(
        string id, bool permanent = false, CancellationToken ct = default)
    {
        var url = $"{AssetsUrl()}/{id}" + (permanent ? "?permanent=true" : string.Empty);
        var request = BuildRequest(HttpMethod.Delete, url);
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private async Task<T> SendAndDeserializeAsync<T>(
        HttpRequestMessage request, CancellationToken ct)
    {
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new InvalidOperationException($"Empty Squidex response for {typeof(T).Name}.");
    }

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

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        request.Options.Set(
            new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey),
            _clientName);

        return request;
    }

    private static string ToQueryString(AssetQuery query)
    {
        var sb = new StringBuilder();

        sb.Append($"$skip={query.Skip}");

        if (query.Top.HasValue)
            sb.Append($"&$top={query.Top}");

        if (!string.IsNullOrEmpty(query.Filter))
            sb.Append($"&$filter={Uri.EscapeDataString(query.Filter)}");

        if (!string.IsNullOrEmpty(query.OrderBy))
            sb.Append($"&$orderby={Uri.EscapeDataString(query.OrderBy)}");

        if (!string.IsNullOrEmpty(query.FullText))
            sb.Append($"&$search={Uri.EscapeDataString(query.FullText)}");

        if (query.Tags?.Count > 0)
            foreach (var tag in query.Tags)
                sb.Append($"&tags={Uri.EscapeDataString(tag)}");

        return sb.ToString();
    }

    private string AssetsUrl() =>
        $"{_options.BaseUrl.TrimEnd('/')}/api/assets/{_options.AppName}";

    private static bool IsTransient(HttpStatusCode code) => code is
        HttpStatusCode.RequestTimeout or
        HttpStatusCode.TooManyRequests or
        HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout;

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