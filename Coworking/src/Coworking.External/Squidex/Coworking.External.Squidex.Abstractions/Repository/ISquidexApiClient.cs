using Coworking.External.Squidex.Abstractions.Models;

public interface ISquidexApiClient
{
    /// <summary>Query via JSON (q= param). Default and recommended.</summary>
    Task<ResponseSchema<T>> QueryAsync<T>(
        string schema,
        RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>Query via OData ($filter, $orderby, etc.).</summary>
    Task<ResponseSchema<T>> QueryODataAsync<T>(
        string schema,
        ODataQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>
    /// Query via POST body — avoids URL length limit.
    /// Useful for complex queries. Same JSON format as RequestQuery.
    /// </summary>
    Task<ResponseSchema<T>> QueryPostAsync<T>(
        string schema,
        RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>
    /// Query by IDs — batched automatically to respect URL length limits.
    /// </summary>
    Task<ResponseSchema<T>> GetByIdsAsync<T>(
        string schema,
        IEnumerable<string> ids,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>?> GetByIdAsync<T>(
        string schema, string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> CreateAsync<T>(
        string schema, T data, bool publish = true,
        CancellationToken ct = default);

    Task<ContentDto<T>> UpdateAsync<T>(
        string schema, string id, T data,
        CancellationToken ct = default);

    Task<ContentDto<T>> PatchAsync<T>(
        string schema, string id, T data,
        CancellationToken ct = default);

    Task DeleteAsync(
        string schema, string id, bool permanent = false,
        CancellationToken ct = default);

    Task<ContentDto<T>> ChangeStatusAsync<T>(
        string schema, string id, string newStatus,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetAppLocalesAsync(CancellationToken ct = default);
}