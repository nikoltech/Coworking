using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Pagination;

namespace Coworking.External.Squidex.Repository;

/// <summary>
/// Base Squidex repository. Schema name is fixed per instance.
/// Inherit to add schema-specific query methods.
/// </summary>
public class SquidexRepository<T>(
    ISquidexApiClient client,
    ISquidexPaginator paginator,
    string schema)
    : ISquidexRepository<T> where T : class
{
    protected readonly ISquidexApiClient Client = client;
    protected readonly string Schema = schema;
    private readonly ISquidexPaginator _paginator = paginator;

    public Task<ResponseSchema<T>> QueryAsync(
        RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.QueryAsync<T>(Schema, query, queryOptions, ct);

    public Task<ResponseSchema<T>> GetAllAsync(
        RequestQuery? query = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        _paginator.FetchAllAsync<T>(
            Schema, Client, query ?? RequestQuery.Create(), queryOptions, ct);

    public Task<ContentDto<T>?> GetByIdAsync(
        string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default) =>
        Client.GetByIdAsync<T>(Schema, id, queryOptions, ct);

    public Task<ContentDto<T>> CreateAsync(
        T data, bool publish = true, CancellationToken ct = default) =>
        Client.CreateAsync(Schema, data, publish, ct);

    public Task<ContentDto<T>> UpdateAsync(
        string id, T data, CancellationToken ct = default) =>
        Client.UpdateAsync(Schema, id, data, ct);

    public Task<ContentDto<T>> PatchAsync(
        string id, T data, CancellationToken ct = default) =>
        Client.PatchAsync(Schema, id, data, ct);

    public Task DeleteAsync(
        string id, bool permanent = false, CancellationToken ct = default) =>
        Client.DeleteAsync(Schema, id, permanent, ct);

    public async Task<bool> ExistsAsync(
        object filter,
        bool includeUnpublished = false,
        CancellationToken ct = default)
    {
        var result = await Client.QueryAsync<T>(
            Schema,
            RequestQuery.Create().WithTake(1).WithFilter(filter),
            new QueryOptions { IncludeUnpublished = includeUnpublished, NoSlowTotal = true },
            ct);

        return result.Total > 0;
    }
}