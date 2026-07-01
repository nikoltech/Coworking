using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Exceptions;
using System.Net;
using System.Net.Http.Json;

namespace Coworking.External.Squidex.Client;

/// <summary>
/// Squidex Assets API client.
/// Separate from <see cref="SquidexApiClient"/> — different endpoint, different response shape.
/// Shares transport (auth stamping, retry, deserialization) via <see cref="SquidexHttpClientBase"/>.
/// </summary>
internal sealed class SquidexAssetClient : SquidexHttpClientBase, ISquidexAssetClient
{
    internal SquidexAssetClient(HttpClient http, SquidexAppOptions appOptions, string clientName)
        : base(http, appOptions, clientName)
    {
    }

    public Task<AssetsResponse> QueryAsync(
        AssetQuery? query = null,
        CancellationToken ct = default)
    {
        var uri = new UriBuilder(AssetsUrl()) { Query = ToQueryString(query ?? AssetQuery.Create()) }.Uri;
        var request = CreateRequest(HttpMethod.Get, uri);
        return SendAndDeserializeAsync<AssetsResponse>(request, ct);
    }

    public async Task<AssetDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var request = CreateRequest(HttpMethod.Get, $"{AssetsUrl()}/{id}");
        var response = await SendWithRetryAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await response.EnsureSquidexSuccessAsync(ct);
        return await response.Content.ReadFromJsonAsync<AssetDto>(Json, ct);
    }

    public async Task<AssetDto> UploadAsync(Stream stream, string fileName,
        string mimeType,
        CancellationToken ct = default)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", fileName);

        var request = CreateRequest(HttpMethod.Post, AssetsUrl());
        request.Content = content;

        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<AssetDto>(Json, ct)
               ?? throw new InvalidOperationException("Empty response from Squidex Assets.");
    }

    public async Task<AssetDto> UpdateMetadataAsync(
        string id, UpdateAssetRequest update,
        CancellationToken ct = default)
    {
        var request = CreateRequest(HttpMethod.Put, $"{AssetsUrl()}/{id}");
        request.Content = JsonContent.Create(update, options: Json);

        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);

        return await response.Content.ReadFromJsonAsync<AssetDto>(Json, ct)
               ?? throw new InvalidOperationException("Empty response from Squidex Assets.");
    }

    public async Task DeleteAsync(string id,
        bool permanent = false,
        CancellationToken ct = default)
    {
        var url = $"{AssetsUrl()}/{id}" + (permanent ? "?permanent=true" : string.Empty);
        var request = CreateRequest(HttpMethod.Delete, url);
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private string AssetsUrl() =>
        $"{AppOptions.BaseUrl.TrimEnd('/')}/api/assets/{AppOptions.AppName}";

    private static string ToQueryString(AssetQuery query)
    {
        // Param names ($skip, $top, ...) stay literal; only values are escaped.
        // Assets differ from content OData: $skip is always sent and `tags` repeats without a $ prefix.
        var parts = new List<string> { $"$skip={query.Skip}" };

        if (query.Top.HasValue)                    parts.Add($"$top={query.Top.Value}");
        if (!string.IsNullOrEmpty(query.Filter))   parts.Add($"$filter={Uri.EscapeDataString(query.Filter)}");
        if (!string.IsNullOrEmpty(query.OrderBy))  parts.Add($"$orderby={Uri.EscapeDataString(query.OrderBy)}");
        if (!string.IsNullOrEmpty(query.FullText)) parts.Add($"$search={Uri.EscapeDataString(query.FullText)}");

        if (query.Tags is { Count: > 0 })
            parts.AddRange(query.Tags.Select(tag => $"tags={Uri.EscapeDataString(tag)}"));

        return string.Join('&', parts);
    }
}
