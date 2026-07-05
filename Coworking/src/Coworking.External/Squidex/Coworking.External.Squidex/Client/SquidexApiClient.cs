using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.Localization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Client;

internal sealed class SquidexApiClient : SquidexHttpClientBase, ISquidexApiClient
{
    private readonly SquidexLocaleProvider _locales;

    /// <summary>Safe batch size for IDs query — respects URL length limits.</summary>
    private const int IdsBatchSize = 80;

    /// <summary>Write-only options for the JSON serialized into the ?q= query string.</summary>
    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal SquidexApiClient(HttpClient http, SquidexAppOptions appOptions,
        string clientName,
        SquidexLocaleProvider locales)
        : base(http, appOptions, clientName)
    {
        _locales = locales;
    }

    // ── JSON query ────────────────────────────────────────────────────────────

    /// <summary>
    /// Queries content items by a JSON filter serialized into the URL query string.
    /// </summary>
    /// <remarks>
    /// For complex filters whose serialized JSON exceeds ~2 KB, prefer
    /// <see cref="QueryPostAsync{T}"/> to avoid a 414 URI Too Long error.
    /// </remarks>
    public Task<ResponseSchema<T>> QueryAsync<T>(string schema, RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        var uri = new UriBuilder(ContentUrl(schema)) { Query = ToJsonQueryString(query) }.Uri;
        var request = BuildRequest(HttpMethod.Get, uri, queryOptions);

        return SendAndDeserializeAsync<ResponseSchema<T>>(request, ct);
    }

    // ── OData query ───────────────────────────────────────────────────────────

    public Task<ResponseSchema<T>> QueryODataAsync<T>(string schema, ODataQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        var uri = new UriBuilder(ContentUrl(schema)) { Query = ToODataQueryString(query) }.Uri;
        var request = BuildRequest(HttpMethod.Get, uri, queryOptions);

        return SendAndDeserializeAsync<ResponseSchema<T>>(request, ct);
    }

    // ── POST query ────────────────────────────────────────────────────────────

    public Task<ResponseSchema<T>> QueryPostAsync<T>(string schema, RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(
            HttpMethod.Post, $"{ContentUrl(schema)}/query", queryOptions);

        request.Content = JsonContent.Create(new PostQueryBody(query), options: Json);

        return SendAndDeserializeAsync<ResponseSchema<T>>(request, ct);
    }

    // ── IDs query (batched) ───────────────────────────────────────────────────

    public async Task<ResponseSchema<T>> GetByIdsAsync<T>(string schema, IEnumerable<string> ids,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var tasks = ids.Chunk(IdsBatchSize)
            .Select(batch => QueryByIdsBatchWithCancelAsync<T>(schema, batch, queryOptions, cts));

        var results = await Task.WhenAll(tasks);
        var allItems = results.SelectMany(r => r.Items).ToList();

        return new ResponseSchema<T>(allItems.Count, allItems);
    }

    // ── Single item ───────────────────────────────────────────────────────────

    public async Task<ContentDto<T>?> GetByIdAsync<T>(string schema, string id,
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

    /// <summary>
    /// For conditional GET — client caches ETag and sends it as If-None-Match header.
    /// </summary>
    /// <param name="knownVersion">Optional ETag for conditional GET</param>
    /// <returns>If content is not modified, returns NotModified=true and null content. Otherwise, returns content with NotModified=false.</returns>
    public async Task<(ContentDto<T>? Content, bool NotModified)> GetByIdConditionalAsync<T>(
        string schema, string id,
        int? knownVersion = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Get, $"{ContentUrl(schema)}/{id}", queryOptions);

        if (knownVersion.HasValue)
            request.Headers.IfNoneMatch.Add(
                new EntityTagHeaderValue($"\"{knownVersion}\""));

        var response = await SendWithRetryAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotModified)
            return (null, NotModified: true);

        await response.EnsureSquidexSuccessAsync(ct);

        var content = await response.Content.ReadFromJsonAsync<ContentDto<T>>(Json, ct);
        return (content, NotModified: false);
    }

    // ── Mutations ────────────────────────────────────────────────────────────

    public Task<ContentDto<T>> CreateAsync<T>(string schema, T data,
        bool publish = true,
        CancellationToken ct = default)
    {
        var url = ContentUrl(schema) + (publish ? "?publish=true" : string.Empty);
        var request = BuildRequest(HttpMethod.Post, url);
        request.Content = JsonContent.Create(data, options: Json);
        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    /// <summary>
    /// Updates content item with optimistic concurrency control using ETag.
    /// </summary>
    /// <param name="expectedVersion">Optional ETag for concurrency control</param>
    /// <returns></returns>
    public Task<ContentDto<T>> UpdateAsync<T>(string schema, string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Put, $"{ContentUrl(schema)}/{id}");
        request.Content = JsonContent.Create(data, options: Json);

        // if version is provided, add If-Match header
        if (expectedVersion.HasValue)
            request.Headers.IfMatch.Add(
                new EntityTagHeaderValue($"\"{expectedVersion}\""));

        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    /// <summary>
    /// Partially updates content item with optimistic concurrency control using ETag.
    /// </summary>
    /// <param name="expectedVersion">Optional ETag for concurrency control</param>
    public Task<ContentDto<T>> PatchAsync<T>(
        string schema, string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Patch, $"{ContentUrl(schema)}/{id}");
        request.Content = JsonContent.Create(data, options: Json);

        if (expectedVersion.HasValue)
            request.Headers.IfMatch.Add(
                new EntityTagHeaderValue($"\"{expectedVersion}\""));

        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    public async Task DeleteAsync(string schema, string id,
        bool permanent = false,
        CancellationToken ct = default)
    {
        var url = $"{ContentUrl(schema)}/{id}" + (permanent ? "?permanent=true" : string.Empty);
        var request = BuildRequest(HttpMethod.Delete, url);
        var response = await SendWithRetryAsync(request, ct);
        await response.EnsureSquidexSuccessAsync(ct);
    }

    public Task<ContentDto<T>> ChangeStatusAsync<T>(string schema, string id,
        string newStatus,
        CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Put, $"{ContentUrl(schema)}/{id}/status");
        request.Content = JsonContent.Create(new { status = newStatus }, options: Json);
        return SendAndDeserializeAsync<ContentDto<T>>(request, ct);
    }

    // ── App ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SquidexLocaleInfo>> GetAppLocalesAsync(CancellationToken ct = default)
    {
        var url = $"{AppOptions.BaseUrl.TrimEnd('/')}/api/apps/{AppOptions.AppName}/languages";
        var request = BuildRequest(HttpMethod.Get, url);
        var response = await SendWithRetryAsync(request, ct);

        await response.EnsureSquidexSuccessAsync(ct);

        var result = await response.Content
            .ReadFromJsonAsync<AppLanguagesResponse>(Json, ct);

        return result?.Items
            .Select(l => new SquidexLocaleInfo(l.Iso2Code, l.IsMaster, l.IsOptional))
            .ToList() ?? [];
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private Task<ResponseSchema<T>> QueryByIdsBatchAsync<T>(string schema, string[] batch,
        QueryOptions? queryOptions,
        CancellationToken ct)
    {
        var ids = string.Join(",", batch);
        var request = BuildRequest(
            HttpMethod.Get,
            $"{ContentUrl(schema)}?ids={Uri.EscapeDataString(ids)}",
            queryOptions);

        return SendAndDeserializeAsync<ResponseSchema<T>>(request, ct);
    }

    private async Task<ResponseSchema<T>> QueryByIdsBatchWithCancelAsync<T>(string schema, string[] batch,
        QueryOptions? queryOptions,
        CancellationTokenSource cts)
    {
        try
        {
            return await QueryByIdsBatchAsync<T>(schema, batch, queryOptions, cts.Token);
        }
        catch
        {
            cts.Cancel();
            throw;
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url,
        QueryOptions? queryOptions = null) =>
        ApplyQueryHeaders(CreateRequest(method, url), queryOptions);

    private HttpRequestMessage BuildRequest(HttpMethod method, Uri uri,
        QueryOptions? queryOptions = null) =>
        ApplyQueryHeaders(CreateRequest(method, uri), queryOptions);

    private HttpRequestMessage ApplyQueryHeaders(HttpRequestMessage request,
        QueryOptions? queryOptions)
    {
        var opts = queryOptions ?? QueryOptions.Default;

        if (opts.IncludeUnpublished)
            request.Headers.Add(SquidexRequestHeaders.Unpublished, "true");

        if (opts.NoSlowTotal)
            request.Headers.Add(SquidexRequestHeaders.NoSlowTotal, "true");

        if (opts.Flatten)
        {
            request.Headers.Add(SquidexRequestHeaders.Flatten, "true");
            var languages = opts.Languages ?? [_locales.DefaultLocale];
            request.Headers.Add(SquidexRequestHeaders.Languages, string.Join(",", languages));
        }
        else
        {
            var languages = opts.Languages ?? _locales.SupportedLocales;
            if (languages.Count > 0)
                request.Headers.Add(SquidexRequestHeaders.Languages, string.Join(",", languages));
        }

        return request;
    }

    private static string ToJsonQueryString(RequestQuery query)
    {
        var json = JsonSerializer.Serialize(query, JsonWrite);
        return "q=" + Uri.EscapeDataString(json);
    }

    private static string ToODataQueryString(ODataQuery query)
    {
        // Param names ($top, $filter, ...) must stay literal — OData servers match on
        // them unescaped. Values are escaped in one place below, so a new param can't
        // be added here without going through Uri.EscapeDataString.
        var parameters = new Dictionary<string, string>();

        if (query.Top.HasValue)        parameters["$top"]     = query.Top.Value.ToString();
        if (query.Skip > 0)            parameters["$skip"]    = query.Skip.ToString();
        if (query.Filter  is not null) parameters["$filter"]  = query.Filter;
        if (query.OrderBy is not null) parameters["$orderby"] = query.OrderBy;
        if (query.Search  is not null) parameters["$search"]  = query.Search;

        return string.Join('&', parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    private string ContentUrl(string schema) =>
        $"{AppOptions.BaseUrl.TrimEnd('/')}/api/content/{AppOptions.AppName}/{schema}";

    // ── Response types ────────────────────────────────────────────────────────

    private sealed record PostQueryBody(
        [property: JsonPropertyName("q")] RequestQuery Query);

    private sealed record AppLanguagesResponse(
        [property: JsonPropertyName("items")] List<AppLanguage> Items);

    private sealed record AppLanguage(
        [property: JsonPropertyName("iso2Code")] string Iso2Code,
        [property: JsonPropertyName("isMaster")] bool IsMaster,
        [property: JsonPropertyName("isOptional")] bool IsOptional);
}
