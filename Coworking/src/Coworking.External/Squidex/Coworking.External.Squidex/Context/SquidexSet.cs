using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;

namespace Coworking.External.Squidex.Context;

/// <summary>
/// Queryable and writable set for a Squidex schema.
/// Analogous to EF DbSet&lt;T&gt;.
/// Returned by SquidexContext.Set&lt;T&gt;(schema).
/// </summary>
public class SquidexSet<T> : ISquidexRepository<T> where T : class
{
    protected readonly ISquidexApiClient Client;
    protected readonly ISquidexPaginator Paginator;
    protected readonly string Schema;

    public SquidexSet(ISquidexApiClient client, ISquidexPaginator paginator, string schema)
    {
        Client = client;
        Paginator = paginator;
        Schema = schema;
    }

    public Task<ResponseSchema<T>> QueryAsync(RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.QueryAsync<T>(Schema, query, queryOptions, ct);

    public Task<ResponseSchema<T>> QueryODataAsync(ODataQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.QueryODataAsync<T>(Schema, query, queryOptions, ct);

    public Task<ResponseSchema<T>> QueryPostAsync(RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.QueryPostAsync<T>(Schema, query, queryOptions, ct);

    public Task<ResponseSchema<T>> GetAllAsync(RequestQuery? query = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Paginator.FetchAllAsync<T>(Schema, Client,
            query ?? RequestQuery.Create(),
            Client.AppOptions.MaxPageSize,
            queryOptions,
            ct);

    public Task<ResponseSchema<T>> GetByIdsAsync(IEnumerable<string> ids,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.GetByIdsAsync<T>(Schema, ids, queryOptions, ct);

    public Task<ContentDto<T>?> GetByIdAsync(string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.GetByIdAsync<T>(Schema, id, queryOptions, ct);

    /// <summary>
    /// Gets content by ID, returning a flag indicating if the content has not been modified since the known version.
    /// </summary>
    /// <param name="knownVersion">Optional ETag for conditional GET.</param>
    /// <returns>If content is not modified, returns NotModified=true and null content. Otherwise, returns content with NotModified=false.</returns>
    public Task<(ContentDto<T>? Content, bool NotModified)> GetByIdConditionalAsync(string id,
        int? knownVersion = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.GetByIdConditionalAsync<T>(Schema, id, knownVersion, queryOptions, ct);

    public Task<ContentDto<T>> CreateAsync(T data,
        bool publish = true,
        CancellationToken ct = default) =>
        Client.CreateAsync(Schema, data, publish, ct);

    /// <summary>
    /// Updates content with optimistic concurrency control using ETag.
    /// </summary>
    /// <param name="expectedVersion">optional ETag for concurrency control</param>
    /// <returns></returns>
    public Task<ContentDto<T>> UpdateAsync(string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default) =>
        Client.UpdateAsync(Schema, id, data, expectedVersion, ct);

    public Task<ContentDto<T>> PatchAsync(string id, T data, CancellationToken ct = default) =>
        Client.PatchAsync(Schema, id, data, ct);

    public Task DeleteAsync(string id,
        bool permanent = false,
        CancellationToken ct = default) =>
        Client.DeleteAsync(Schema, id, permanent, ct);

    public async Task<bool> ExistsAsync(object filter,
        bool includeUnpublished = false,
        CancellationToken ct = default)
    {
        var result = await Client.QueryAsync<T>(Schema, RequestQuery.Create().WithTake(1).WithFilter(filter),
            new QueryOptions { IncludeUnpublished = includeUnpublished, NoSlowTotal = true },
            ct);

        return result.Total > 0;
    }
}