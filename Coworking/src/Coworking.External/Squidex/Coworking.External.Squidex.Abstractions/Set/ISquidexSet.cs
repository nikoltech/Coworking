using Coworking.External.Squidex.Abstractions.Models;

namespace Coworking.External.Squidex.Abstractions.Set;

public interface ISquidexSet<T> where T : class
{
    Task<ResponseSchema<T>> QueryAsync(RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>OData query — slash path separator (data/Title/iv).</summary>
    Task<ResponseSchema<T>> QueryODataAsync(ODataQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>POST body query — avoids URL length limit for complex queries.</summary>
    Task<ResponseSchema<T>> QueryPostAsync(RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ResponseSchema<T>> GetAllAsync(RequestQuery? query = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>Fetch by IDs — batched at 80 IDs per request.</summary>
    Task<ResponseSchema<T>> GetByIdsAsync(IEnumerable<string> ids,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>?> GetByIdAsync(string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>
    /// Conditional GET. Pass the ETag from a previous call as If-None-Match, and keep the one
    /// returned here for the next. Squidex matches on a content hash, not on the item's version.
    /// </summary>
    /// <returns>NotModified=true with null content when unchanged; otherwise the content and its ETag.</returns>
    Task<(ContentDto<T>? Content, string? ETag, bool NotModified)> GetByIdConditionalAsync(string id,
        string? knownETag = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> CreateAsync(T data,
        bool publish = true,
        CancellationToken ct = default);

    Task<ContentDto<T>> UpdateAsync(string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> PatchAsync(string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default);

    Task DeleteAsync(string id,
        bool permanent = false,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(object filter,
        bool includeUnpublished = false,
        CancellationToken ct = default);
}